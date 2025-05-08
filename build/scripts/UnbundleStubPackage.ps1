param(
    [Parameter(Mandatory)]
    [string]
    $InputPath,

    [Parameter(Mandatory)]
    [string]
    $OutputLocation,

    [Parameter(HelpMessage="Path to makeappx.exe")]
    [ValidateScript({Test-Path $_ -Type Leaf})]
    [string]
    $MakeAppxPath = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x86\MakeAppx.exe"
)

function Unbundle-AppxBundle
{
    param($tool, $bundlePath, $toPath)

    $makeAppxCmd = ("& '" + $tool + "' unbundle /p " + $bundlePath + " /d '" + $toPath + "'")

    Write-Host $makeAppxCmd
    Invoke-Expression $makeAppxCmd
}

# Unbundle stub appxbundle
Write-Host ("Input folder:" + $InputPath)
$stubBundles = Get-ChildItem $InputPath -recurse | Where-Object {$_.extension -eq ".msixbundle"}
if ($stubBundles.count -ne 1)
{
    Write-Host -ForegroundColor RED $stubBundles.count + " stub appxbundle bundles found"
    exit 1
}

$stubBundle = $stubBundles | Select-Object -First 1
Write-Host("Stub bundle path:" + $stubBundle.FullName)
Unbundle-AppxBundle $MakeAppxPath $stubBundle.FullName $OutputLocation