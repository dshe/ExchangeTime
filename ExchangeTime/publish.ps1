Set-Location $PSScriptRoot
#$folder = "./publish"
$folder = "Z:\data\prg\apps\ExchangeTime"
Remove-Item "$folder\*" -Force -Recurse
dotnet publish -c Release -o "$folder"
