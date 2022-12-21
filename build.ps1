Remove-Item -Path (Join-Path $PSScriptRoot bin)

dotnet publish -r linux-x64 -p:PublishSingleFile=true --self-contained true -o bin/linux -c release
dotnet publish -r win-x64 -p:PublishSingleFile=true --self-contained true -o bin/windows -c release