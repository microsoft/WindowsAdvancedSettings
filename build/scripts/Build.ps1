Param(
    [string]$Platform = "x64",
    [string]$Configuration = "debug",
    [string]$Version,
    [string]$BuildStep = "all",
    [string]$AzureBuildingBranch = "main",
    [switch]$IsAzurePipelineBuild = $false,
    [switch]$Help = $false
)

$StartTime = Get-Date

if ($Help) {
    Write-Host @"
Copyright (c) Microsoft Corporation.
Licensed under the MIT License.

Syntax:
      Build.cmd [options]

Description:
      Builds GIT services for Windows.

Options:

  -Platform <platform>
      Only build the selected platform(s)
      Example: -Platform x64
      Example: -Platform "x86,x64,arm64"

  -Configuration <configuration>
      Only build the selected configuration(s)
      Example: -Configuration release
      Example: -Configuration "debug,release"

  -Help
      Display this usage message.
"@
  Exit
}

# Install NuGet Cred Provider
Invoke-Expression "& { $(irm https://aka.ms/install-artifacts-credprovider.ps1) } -AddNetfx"

# Root is two levels up from the script location.
$env:Build_RootDirectory = (Get-Item $PSScriptRoot).parent.parent.FullName
$env:Build_Platform = $Platform.ToLower()
$env:Build_Configuration = $Configuration.ToLower()
$env:msix_version = build\Scripts\CreateBuildInfo.ps1 -Version $Version -IsAzurePipelineBuild $IsAzurePipelineBuild
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] 'Administrator')

if ($IsAzurePipelineBuild) {
  Copy-Item (Join-Path $env:Build_RootDirectory "build\nuget.config.internal") -Destination (Join-Path $env:Build_RootDirectory "nuget.config")
}

$msbuildPath = &"${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -prerelease -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe

$ErrorActionPreference = "Stop"

. (Join-Path $env:Build_RootDirectory "build\scripts\CertSignAndInstall.ps1")

Try {
  if (($BuildStep -ieq "all") -Or ($BuildStep -ieq "msix")) {
    $buildRing = "Dev"
    $appxmanifestPath = (Join-Path $env:Build_RootDirectory "src\AdvancedSettings\Package-Dev.appxmanifest")

    if (($AzureBuildingBranch -ieq "release") -Or ($env:Build_Configuration -ieq "release")) {
      $buildRing = "Stable"
      $appxmanifestPath = (Join-Path $env:Build_RootDirectory "src\AdvancedSettings\Package.appxmanifest")
    }

    [Reflection.Assembly]::LoadWithPartialName("System.Xml.Linq")
    $xIdentity = [System.Xml.Linq.XName]::Get("{http://schemas.microsoft.com/appx/manifest/foundation/windows10}Identity");

    # Update the appxmanifest
    $appxmanifest = [System.Xml.Linq.XDocument]::Load($appxmanifestPath)
    $appxmanifest.Root.Element($xIdentity).Attribute("Version").Value = $env:msix_version
    $appxmanifest.Save($appxmanifestPath)

    foreach ($platform in $env:Build_Platform.Split(",")) {
      foreach ($configuration in $env:Build_Configuration.Split(",")) {
        $appxPackageDir = (Join-Path $env:Build_RootDirectory "AppxPackages\$configuration")
        $solutionPath = (Join-Path $env:Build_RootDirectory "WindowsAdvancedSettings.sln")
        $msbuildArgs = @(
            ($solutionPath),
            ("/p:platform="+$platform),
            ("/p:configuration="+$configuration),
            ("/restore"),
            ("/binaryLogger:WindowsAdvancedSettings.$platform.$configuration.binlog"),
            ("/p:AppxPackageOutput=$appxPackageDir\WindowsAdvancedSettings-$platform.msix"),
            ("/p:AppxPackageSigningEnabled=false"),
            ("/p:GenerateAppxPackageOnBuild=true"),
            ("/p:BuildRing=$buildRing")
        )

        & $msbuildPath $msbuildArgs
        if (-not($IsAzurePipelineBuild) -And $isAdmin) {
          # This can fail if SignTool.exe is not found, which is part of the Windows SDK.
          Invoke-SignPackage "$appxPackageDir\WindowsAdvancedSettings-$platform.msix"
        }
      }
    }

    # Reset the appxmanifest to prevent unnecessary code changes
    $appxmanifest = [System.Xml.Linq.XDocument]::Load($appxmanifestPath)
    $appxmanifest.Root.Element($xIdentity).Attribute("Version").Value = "0.0.0.0"
    $appxmanifest.Save($appxmanifestPath)
  }

  if (($BuildStep -ieq "all") -Or ($BuildStep -ieq "msixbundle")) {
    foreach ($configuration in $env:Build_Configuration.Split(",")) {
      .\build\scripts\Create-AppxBundle.ps1 -InputPath (Join-Path $env:Build_RootDirectory "AppxPackages\$configuration") -ProjectName AzureExtension -BundleVersion ([version]$env:msix_version) -OutputPath (Join-Path $env:Build_RootDirectory ("AppxBundles\$configuration\WindowsAdvancedSettings_" + $env:msix_version + "_8wekyb3d8bbwe.msixbundle"))
      if (-not($IsAzurePipelineBuild) -And $isAdmin) {
        # This can fail if SignTool.exe is not found, which is part of the Windows SDK.
        Invoke-SignPackage ("AppxBundles\$configuration\WindowsAdvancedSettings_" + $env:msix_version + "_8wekyb3d8bbwe.msixbundle")
      }
    }
  }
} Catch {
  $formatString = "`n{0}`n`n{1}`n`n"
  $fields = $_, $_.ScriptStackTrace
  Write-Host ($formatString -f $fields) -ForegroundColor RED
  Exit 1
}

$TotalTime = (Get-Date)-$StartTime
$TotalMinutes = [math]::Floor($TotalTime.TotalMinutes)
$TotalSeconds = [math]::Ceiling($TotalTime.TotalSeconds) - ($TotalMinutes * 60)

if (-not($isAdmin)) {
  Write-Host @"

WARNING: Cert signing requires admin privileges.  To sign, run the following in an elevated Developer Command Prompt.
"@ -ForegroundColor GREEN
  foreach ($platform in $env:Build_Platform.Split(",")) {
    foreach ($configuration in $env:Build_Configuration.Split(",")) {
      $appxPackageDir = (Join-Path $env:Build_RootDirectory "AppxPackages\$configuration")
        Write-Host @"
powershell -command "& { . build\scripts\CertSignAndInstall.ps1; Invoke-SignPackage $appxPackageDir\WindowsAdvancedSettings-$platform.msix }"
"@ -ForegroundColor GREEN
    }
  }
}

Write-Host @"

Total Running Time:
$TotalMinutes minutes and $TotalSeconds seconds
"@ -ForegroundColor CYAN