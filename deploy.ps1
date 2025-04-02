# Paths
$projectRoot = "C:\Users\camer\Desktop\Apps\AdminPortal"
$publishPath = "$projectRoot\AdminPortal.Server\bin\Release\net7.0\publish"
$linuxPath = "$projectRoot\AdminPortal.Server\bin\Release\net7.0\linux-x64"

# Compress the wwwroot directory from the publish directory
Write-Host "Compressing wwwroot directory..."
$wwwrootPath = "$publishPath\wwwroot"
$zipPath = "$linuxPath\wwwroot.zip"

if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

# Compress the entire wwwroot directory as a folder (includes the directory itself)
Compress-Archive -Path $wwwrootPath -DestinationPath $zipPath

Write-Host "Compression completed successfully!"