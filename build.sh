#!/bin/sh
rm -rf ./bin
rm -f *.zip

linux_arch="x86 x64 arm arm64"
win_arch="x64"
PAK="Assets/PAKs"
NABUs="Assets/NABUs"
Files="Assets/Files"
CONFIG="Assets/Config"
sudo apt-get install clang zlib1g-dev
publish () {
    os=$1
    arch=$2
    target=${3:-$os}
    BIN="bin/$os/$arch"

    dotnet publish ./Nabu.NetSimWeb -r "${target}-${arch}" -p:PublishSingleFile=true --self-contained true -o $BIN -c release
    
    cp -r $PAK $BIN
    cp -r $NABUs $BIN
    cp -r $Files $BIN
    cp -f $CONFIG/appsettings.$os.json $BIN/appsettings.json
    cp -f README.md $BIN    
   
    zip -r nns-$os-$arch.zip $BIN 1> /dev/null
   
}

#
#for arch in $linux_arch;
#do
#    publish linux $arch
#done
#

for arch in $win_arch;
do
    publish win $arch
done

#publish macos x64 osx.11.0
#publish macos arm64 arm64