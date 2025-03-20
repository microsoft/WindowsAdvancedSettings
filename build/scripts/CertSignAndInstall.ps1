function Invoke-SignPackage([string]$Path) {
    if (-not($Path)) {
        Write-Host "Path parameter cannot be empty"
        return
    }

    if (-not(Test-Path $Path -PathType Leaf)) {
        Write-Host $Path is not a valid file
        return
    }

    $certName = "Microsoft.Windows.WindowsAdvancedSettings"
    $cert = Get-ChildItem 'Cert:\CurrentUser\My' | Where-Object {$_.FriendlyName -match $certName} | Select-Object -First 1

    if ($cert) {
        $expiration = $cert.NotAfter
        $now = Get-Date
        if ( $expiration -lt $now)
        {
            Write-Host "Test certificate for $($cert.Thumbprint)...Expired ($expiration). Replacing it with a new one."
            Remove-Item $cert
            $cert = $Null
        }
    }

    if (-not($cert)) {
        Write-Host "No certificate found. Creating a new certificate for signing."
        $cert = & New-SelfSignedCertificate -Type Custom -Subject "CN=Microsoft Corporation, O=Microsoft Corporation, L=Redmond, S=Washington, C=US" -KeyUsage DigitalSignature -FriendlyName $certName -CertStoreLocation "Cert:\CurrentUser\My" -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")
    }

    SignTool sign /fd SHA256 /sha1 $($cert.Thumbprint) $Path

    if (-not(Test-Path Cert:\LocalMachine\TrustedPeople\$($cert.Thumbprint))) {
        Export-Certificate -Cert $cert -FilePath "$($PSScriptRoot)\Microsoft.Windows.WindowsAdvancedSettings.cer" -Type CERT
        Import-Certificate -FilePath "$($PSScriptRoot)\Microsoft.Windows.WindowsAdvancedSettings.cer" -CertStoreLocation Cert:\LocalMachine\TrustedPeople    
        Remove-Item -Path "$($PSScriptRoot)\Microsoft.Windows.WindowsAdvancedSettings.cer"
        (Get-ChildItem Cert:\LocalMachine\TrustedPeople\$($cert.Thumbprint)).FriendlyName = $certName
    }
}

function Remove-WindowsAdvancedSettingsCertificates() {
    Get-ChildItem 'Cert:\CurrentUser\My' | Where-Object {$_.FriendlyName -match 'Microsoft.Windows.WindowsAdvancedSettings'} | Remove-Item
    Get-ChildItem 'Cert:\LocalMachine\TrustedPeople' | Where-Object {$_.FriendlyName -match 'Microsoft.Windows.WindowsAdvancedSettings'} | Remove-Item
}