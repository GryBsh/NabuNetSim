#!/bin/sh
rm -rf ./bin
rm -f *.zip

linux_arch="x64 arm arm64"
win_arch="x64 arm arm64"
PAK="Assets/PAKs"
CONFIG="Assets/Config"
sudo apt-get install clang zlib1g-dev
publish () {
    os=$1
    arch=$2
    target=${3:-$os}
    BIN="bin/$os/$arch"

    echo "<| Publishing $os $arch $target |>"
    echo "   dotnet publish -r \"${target}-${arch}\" -p:PublishAot=true -p:PublishSingleFile=true -p:StripSymbols=true -o $BIN -c release"
    dotnet publish -r "${target}-${arch}" -p:PublishAot=true -p:PublishSingleFile=true -p:StripSymbols=true -o $BIN -c release 1>/dev/null
    echo "   Preparing $BIN contents"
    cp -r $PAK $BIN
    mkdir $BIN/NABUs
    cp -f $CONFIG/appsettings.$os.json $BIN/appsettings.json
    cp -f README.md $BIN
    echo "   Zipping $BIN"
    zip -r nnae-$os-$arch.zip $BIN 1>/dev/null

    echo "<| Published $os $arch $target |>"
}

for arch in $linux_arch;
do
    publish linux $arch
done

for arch in $win_arch;
do
    publish win $arch
done

#publish macos x64 osx
#publish macos arm64 osx.11.0