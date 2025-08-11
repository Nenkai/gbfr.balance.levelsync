# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/gbfr.balance.levelsync/*" -Force -Recurse
dotnet publish "./gbfr.balance.levelsync.csproj" -c Release -o "$env:RELOADEDIIMODS/gbfr.balance.levelsync" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location