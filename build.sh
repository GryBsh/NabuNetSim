#webproject='NetSimWeb'
os='win'
archs='x64'
project="NetSimWeb"
PAK='Assets/PAKs'
PAK_output='PAKs'
#PAKs='direct cycle1/raw cycle2/raw cycle2ex'
NABUs='Assets/NABUs'
#Files=Assets/Files
Packages='Assets/Packages'
CONFIG='Assets/Config'
win_start='Assets/start.cmd'

name='nns'

for arch in $archs; 
do
    BIN="bin/$os/$arch"

    dotnet publish ./Nabu.$project -r $os-$arch -p:PublishSingleFile=true --self-contained true -o $BIN -c release
    
    cp -r $NABUs $BIN
    cp -r $Packages $BIN
    mkdir $BIN/Files
    mkdir $BIN/Files/Source
    
    if [[ $os == osx* ]]; then
        cp -f $CONFIG/appsettings.macos.json $BIN/appsettings.json
    else
        cp -f $CONFIG/appsettings.$os.json $BIN/appsettings.json
    fi

    cp -f README.md $BIN
    cp -f LICENSE $BIN

    if [ $os == 'win' ]; then
        cp -f $win_start $BIN
    fi

    rm -f $BIN/*.pdb
    rm -f $BIN/appsettings.Development.json
    cd $BIN
    zip -r ../../../$name-$os-$arch.zip *
    cd ../../../
done