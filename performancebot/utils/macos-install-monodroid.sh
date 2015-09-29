#!/bin/bash

set -o pipefail
set -e
set -x

if [ $# -ne 4 ]; then
    echo "usage: $0 <monodroid.pkg> <submitscript> <benchmarkerToolsDir> <commit>"
    exit 1
fi

function finish {
    diskutil umount force "$VOLPATH" || true
    if [ ! "x$TMP_DIR" = "x" ]; then
        rm -rf $TMP_DIR
    fi
}

MONODROID_PKG=$1
SUBMITSCRIPT=$2
BENCHMARKERTOOLS=$3
COMMITID=$4

if [ ! -f $SUBMITSCRIPT ]; then
    echo "file doesn't exist: $SUBMITSCRIPT"
    exit 2
fi

if [ ! -d $BENCHMARKERTOOLS ]; then
    echo "directory doesn't exist: $BENCHMARKERTOOLS"
    exit 3
fi


VOLNAME=MonoDroidInstallation$RANDOM
VOLPATH=/Volumes/$VOLNAME

TMP_DIR=$(mktemp -d /tmp/install-monodroid.XXXXXX)
trap finish EXIT


DMGPATH="$TMP_DIR/installation.dmg"
hdiutil create "$DMGPATH" -volname "$VOLNAME" -size 1g -fs HFSX -attach

# Skip browser opening if /tmp/MONODROID_HEADLESS_INSTALL exists
DELETE_HEADLESS=0
if [ ! -f /tmp/MONODROID_HEADLESS_INSTALL ]; then
    touch /tmp/MONODROID_HEADLESS_INSTALL
    DELETE_HEADLESS=1
fi

sudo installer -package "$MONODROID_PKG" -target "$VOLPATH"

if [ $DELETE_HEADLESS = '1' ]; then
    rm /tmp/MONODROID_HEADLESS_INSTALL
fi

# pointing xbuild to the temporary installation
export XBUILD_FRAMEWORK_FOLDERS_PATH=$VOLPATH/Library/Frameworks/Mono.framework/External/xbuild-frameworks
export MSBuildExtensionsPath=$VOLPATH/Library/Frameworks/Mono.framework/External/xbuild
export MONO_ANDROID_PATH=$VOLPATH/Library/Frameworks/Xamarin.Android.framework/Versions/Current

bash $SUBMITSCRIPT $BENCHMARKERTOOLS $COMMITID

