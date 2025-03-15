Param(
    [string]$Platform = "x64",
    [string]$Configuration = "debug",
    [switch]$IsAzurePipelineBuild = $false,
    [switch]$Help = $false
)

$StartTime = Get-Date

if ($Help) {
    Write-Host @"
Copyright (c) Microsoft Corporation.
Licensed under the MIT License.

Syntax:
      Test.cmd [options]

Description:
      Runs AzureExtension tests.

Options:

  -Platform <platform>
      Only buil the selected platform(s)
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

# Root is two levels up from the script location.
$env:Build_SourcesDirectory = (Get-Item $PSScriptRoot).parent.parent.FullName
$env:Build_Platform = $Platform.ToLower()
$env:Build_Configuration = $Configuration.ToLower()

$vstestPath = &"${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -prerelease -products * -find **\TestPlatform\vstest.console.exe

$ErrorActionPreference = "Stop"

$isInstalled = Get-ChildItem HKLM:\SOFTWARE\$_\Microsoft\Windows\CurrentVersion\Uninstall\ | ? {($_.GetValue("DisplayName")) -like "*Windows Application Driver*"}

if (-not($IsAzurePipelineBuild)) {
  if ($isInstalled){
    Write-Host "WinAppDriver is already installed on this computer."
  }
  else {
    Write-Host "WinAppDriver will be installed in the background."
    $url = "https://github.com/microsoft/WinAppDriver/releases/download/v1.2.99/WindowsApplicationDriver-1.2.99-win-x64.exe"
    $outpath = "$env:Build_SourcesDirectory\temp"
    Invoke-WebRequest -Uri $url -OutFile "$env:Build_SourcesDirectory\temp\WinAppDriverx64.exe"

    Start-Process -Wait -Filepath $env:Build_SourcesDirectory\WinAppDriverx64.exe -ArgumentList "/S" -PassThru
  }

  start-Process -FilePath "C:\Program Files\Windows Application Driver\WinAppDriver.exe" 
}

Try {
  foreach ($platform in $env:Build_Platform.Split(",")) {
    foreach ($configuration in $env:Build_Configuration.Split(",")) {
      # TODO: UI tests are currently disabled in pipeline until signing is solved
      if (-not($IsAzurePipelineBuild)) {
        $Package = Get-AppPackage "WindowsAdvancedSettings"
        if ($Package) {
          Write-Host "Uninstalling old AzureExtension"
          Remove-AppPackage -Package $Package.PackageFullName
        }
        Write-Host "Installing AzureExtension"
        Add-AppPackage "AppxPackages\$platform\$configuration\WindowsAdvancedSettings.msix"
      }

      $vstestArgs = @(
          ("/Platform:$platform"),
          ("/Logger:trx;LogFileName=AzureExtension.Test-$platform-$configuration.trx"),
          ("/TestCaseFilter:""TestCategory=Unit"""),
          ("BuildOutput\$configuration\$platform\AzureExtension.Test\AzureExtension.Test.dll")
      )
      $winAppTestArgs = @(
          ("/Platform:$platform"),
          ("/Logger:trx;LogFileName=AzureExtension.UITest-$platform-$configuration.trx"),
          ("BuildOutput\$configuration\$platform\AzureExtension.UITest\AzureExtension.UITest.dll")
      )

      & $vstestPath $vstestArgs
      # TODO: UI tests are currently disabled in pipeline until signing is solved
      if (-not($IsAzurePipelineBuild)) {
          & $vstestPath $winAppTestArgs
      }
    }
  }
} Catch {
  $formatString = "`n{0}`n`n{1}`n`n"
  $fields = $_, $_.ScriptStackTrace
  Write-Host ($formatString -f $fields) -ForegroundColor RED
  Exit 1
}

if (-not($IsAzurePipelineBuild)) {
  Stop-Process -Name "WinAppDriver"
}

$TotalTime = (Get-Date)-$StartTime
$TotalMinutes = [math]::Floor($TotalTime.TotalMinutes)
$TotalSeconds = [math]::Ceiling($TotalTime.TotalSeconds)

Write-Host @"

Total Running Time:
$TotalMinutes minutes and $TotalSeconds seconds
"@ -ForegroundColor CYAN