#!/bin/bash

VOLNAME=MonoInstallation$RANDOM
VOLPATH=/Volumes/$VOLNAME
GLOBAL_MONO=mono-sgen

usage () {
    echo "Usage: run.sh [options]"
    echo "Options:"
    echo "    -r,--root PATH	Root of benchmarker repo"
    echo "    -f,--file PATH	Mono installer .pkg file"
    echo "    -u,--url URL	URL of Mono installer .pkg"
    echo "    -c,--config NAME	Configuration name"
    echo "    -s,--commit SHA	Commit of the Mono package"
    exit $1
}

while [ "$1" != "" ]; do
    case $1 in
	-r | --root )		shift
				BENCHMARKER_ROOT=$1
				;;
        -f | --file )           shift
                                MONO_PKG=$1
                                ;;
        -u | --url )            shift
                                MONO_PKG_URL=$1
                                ;;
	-c | --config )		shift
				CONFIG_NAME=$1
				;;
	-s | --commit )		shift
				COMMIT=$1
				;;
        -h | --help )           usage 0
                                ;;
        * )                     usage 1
    esac
    shift
done

if [ "x$CONFIG_NAME" = "x" ] ; then
    echo "Error: No configuration name given"
    exit 1
fi

if [ "x$MONO_PKG" = "x" -a "x$MONO_PKG_URL" = "x" ] ; then
    echo "Error: No package file or URL given"
    exit 1
fi

if [ "x$COMMIT" = "x" ] ; then
    echo "Error: No commit given"
    exit 1
fi

if [ "x$BENCHMARKER_ROOT" = "x" ] ; then
    BENCHMARKER_ROOT=`git rev-parse --show-toplevel`
    if [ $? -ne 0 ] ; then
	echo "Error: Could not find benchmarker repo root"
	exit 1
    fi
fi

COMPARE_EXE=$BENCHMARKER_ROOT/tools/compare.exe

if [ ! -f "$COMPARE_EXE" ] ; then
    echo "Error: Cannot access executable $COMPARE_EXE"
    exit 1
fi

CONFIG_PATH="$BENCHMARKER_ROOT/configs/$CONFIG_NAME.conf"

if [ ! -f "$CONFIG_PATH" ] ; then
    echo "Error: Cannot access config file $CONFIG_PATH"
    exit 1
fi

TMP_DIR="/tmp/benchmarker.$RANDOM$RANDOM"
mkdir "$TMP_DIR"
if [ $? -ne 0 ] ; then
    echo "Error: Cannot create temporary directory $TMP_DIR"
    exit 1
fi

ERROR=0
MOUNTED=0

finish () {
    if [ $MOUNTED -ne 0 ] ; then
	diskutil umount force "$VOLPATH"
	if [ $? -ne 0 ] ; then
	    echo "Error: Cannot unmount disk image"
	    ERROR=1
	fi
    fi

    rm -rf "$TMP_DIR"
    if [ $? -ne 0 ] ; then
	echo "Error: Cannot delete temporary directory $TMP_DIR"
	ERROR=1
    fi

    exit $ERROR
}

error () {
    echo "$@"
    ERROR=1
    finish
}

if [ "x$MONO_PKG" = "x" ] ; then
    MONO_PKG="$TMP_DIR/installer.pkg"
    curl -o "$MONO_PKG" "$MONO_URL"
    if [ $? -ne 0 ] ; then
	error "Error: Could not download Mono package from $MONO_URL"
    fi
fi

DMGPATH="$TMP_DIR/installation.dmg"
hdiutil create "$DMGPATH" -volname "$VOLNAME" -size 1g -fs HFSX -attach
if [ $? -ne 0 ] ; then
    echo Error creating or mounting disk image
    exit 1
fi
MOUNTED=1

sudo installer -package "$MONO_PKG" -target "$VOLPATH"
if [ $? -ne 0 ] ; then
    echo Error installing Mono package
    ERROR=1
    finish
fi

VERSION=`ls "$VOLPATH/Library/Frameworks/Mono.framework/Versions" | grep -v Current`
if [ $? -ne 0 ] ; then
    error Error figuring out Mono version
fi

if [ `echo "$VERSION" | wc -w` -ne 1 ] ; then
    error No unique Mono version in package
fi

echo Mono version is $VERSION

MONO_ROOT="$VOLPATH/Library/Frameworks/Mono.framework/Versions/$VERSION"
MONO_PATH="$MONO_ROOT/bin/mono-sgen"

"$GLOBAL_MONO" "$COMPARE_EXE" -b ahcbench,db --commit "$COMMIT" --root "$MONO_ROOT" "$BENCHMARKER_ROOT/tests" "$BENCHMARKER_ROOT/benchmarks" "$CONFIG_PATH"
if [ $? -ne 0 ] ; then
    error Error running benchmarks
fi

finish
