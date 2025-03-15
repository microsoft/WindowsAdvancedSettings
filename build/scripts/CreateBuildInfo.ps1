[CmdLetBinding()]
Param(
    [string]$Version,
    [bool]$IsAzurePipelineBuild = $false
)

$Major = "0"
$Minor = "21"
$Patch = "99" # default to 99 for local builds

$versionSplit = $Version.Split(".");
if ($versionSplit.length -gt 3) { $Build = $versionSplit[3] }
if ($versionSplit.length -gt 2) { $Elapsed = $versionSplit[2] }
if ($versionSplit.length -gt 1) { 
    if ($versionSplit[1].length -gt 2) {
        $Minor = $versionSplit[1].SubString(0,$versionSplit[1].length-2);
        $Patch = $versionSplit[1].SubString($versionSplit[1].length-2, 2);
    } else {
        $Minor = $versionSplit[1]
    }
}
if ($versionSplit.length -gt 0) { $Major = $versionSplit[0] }

# Compute/Verify the MSIX version
#
# MSIX Version = M.NPP.E.B
# where
#   M = Major (max <= 65535)
#   N = Minor (max <= 654)
#   P = Patch (max <= 99)
#   E = Elapsed (max <= 65535)
#   B = Build (max <= 65535)
#
# NOTE: Elapsed is the number of days since the epoch (Jan'1, 2023).
# NOTE: Make sure to compute Elapsed using Universal Time (UTC).
#
# NOTE: Build is computed as HHMM i.e. the time (hour and minute) when building locally, 0 otherwise.
#
$epoch = (Get-Date -Year 2023 -Month 1 -Day 1).ToUniversalTime()
$now = (Get-Date).ToUniversalTime()
if ([string]::IsNullOrWhiteSpace($Elapsed)) {
  $Elapsed = $(New-Timespan -Start $epoch -End $now).Days
}
#
$version_h = $now.Hour
$version_m = $now.Minute
if (-not($IsAzurePipelineBuild) -And [string]::IsNullOrWhiteSpace($Build)) {
  $Build = ($version_h * 100 + $version_m).ToString()
}
#
$version_dotquad = [int[]]($Major, ($Minor + $Patch), $Elapsed, $Build)
return ($version_dotquad -Join ".")