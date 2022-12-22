rm -rf ./bin

dotnet publish -r linux-x64 -p:PublishSingleFile=true --self-contained true -o bin/linux/x64 -c release
dotnet publish -r win-x64 -p:PublishSingleFile=true --self-contained true -o bin/windows/x64 -c release
dotnet publish -r osx-x64 -p:PublishSingleFile=true --self-contained true -o bin/osx/x64 -c release

dotnet publish -r linux-arm -p:PublishSingleFile=true --self-contained true -o bin/linux/arm -c release
dotnet publish -r win-arm -p:PublishSingleFile=true --self-contained true -o bin/windows/arm -c release

dotnet publish -r linux-arm64 -p:PublishSingleFile=true --self-contained true -o bin/linux/arm64 -c release
dotnet publish -r win-arm64 -p:PublishSingleFile=true --self-contained true -o bin/windows/arm64 -c release
dotnet publish -r osx.11.0-arm64 -p:PublishSingleFile=true --self-contained true -o bin/osx/arm64 -c release
