param(
    [string]$PrimaryInterface = "Ethernet",
    [string]$VirtualInterface = "Ethernet 2",
    [int]$MobilePort = 5000
)

$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).
    IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Error "Execute este script em um PowerShell aberto como Administrador."
    exit 1
}

Write-Host "Priorizando a interface real: $PrimaryInterface"
Set-NetIPInterface -InterfaceAlias $PrimaryInterface -AddressFamily IPv4 -InterfaceMetric 5
Set-DnsClient -InterfaceAlias $PrimaryInterface -RegisterThisConnectionsAddress $true

Write-Host "Removendo a interface virtual do registro de nome: $VirtualInterface"
Set-NetIPInterface -InterfaceAlias $VirtualInterface -AddressFamily IPv4 -InterfaceMetric 500
Set-DnsClient -InterfaceAlias $VirtualInterface -RegisterThisConnectionsAddress $false

Write-Host "Liberando porta $MobilePort no Firewall do Windows"
$ruleName = "YourRhythm Studio Mobile Dev $MobilePort"
if (-not (Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue)) {
    New-NetFirewallRule `
        -DisplayName $ruleName `
        -Direction Inbound `
        -Action Allow `
        -Protocol TCP `
        -LocalPort $MobilePort `
        -Profile Private | Out-Null
}

Write-Host "Limpando caches de nome locais"
ipconfig /flushdns | Out-Null
nbtstat -R | Out-Null
nbtstat -RR | Out-Null

$hostName = hostname
$primaryIp = Get-NetIPAddress -AddressFamily IPv4 -InterfaceAlias $PrimaryInterface |
    Where-Object { $_.IPAddress -like "192.168.*" } |
    Select-Object -First 1 -ExpandProperty IPAddress

Write-Host ""
Write-Host "Pronto."
Write-Host "URL por nome: http://$hostName`:$MobilePort"
if ($primaryIp) {
    Write-Host "Fallback por IP: http://$primaryIp`:$MobilePort"
}
