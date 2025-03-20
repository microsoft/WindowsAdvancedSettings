# This script unstubs the telemetry at build time and replaces the stubbed file with a reference internal nuget package

Remove-Item "$($PSScriptRoot)\..\..\src\Telemetry\TelemetryEventSource.cs"

$projFile = "$($PSScriptRoot)\..\..\src\Telemetry\WindowsAdvancedSettings.Telemetry.csproj"
$projFileContent = Get-Content $projFile -Encoding UTF8 -Raw

if ($projFileContent.Contains('Microsoft.Telemetry.Inbox.Managed')) {
    Write-Output "Project file already contains a reference to the internal package."
    return;
}

$xml = [System.Xml.XmlDocument]$projFileContent
$xml.PreserveWhitespace = $true
$itemGroup = $xml.CreateElement("ItemGroup")
$packageRef = $xml.CreateElement("PackageReference")
$packageRef.SetAttribute("Include", "Microsoft.Telemetry.Inbox.Managed")
$packageRef.SetAttribute("Version", "10.0.25148.1001-220626-1600.rs-fun-deploy-dev5")
$itemGroup.AppendChild($packageRef)
$xml.Project.AppendChild($itemGroup)
$xml.Save($projFile)