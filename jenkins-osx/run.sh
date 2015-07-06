#!/bin/bash

set -o pipefail

pushd `dirname "$0"` > /dev/null
BENCHMARKER_ROOT=`pwd`
BENCHMARKER_ROOT=`dirname "$BENCHMARKER_ROOT"`
popd > /dev/null

GLOBAL_MONO=mono-sgen

DEB_COMMON_URL=`lynx -dump -hiddenlinks=listonly -listonly http://jenkins.mono-project.com/repo/debian/pool/main/m/mono-snapshot-common/ | awk '$2 ~ /_all\.deb$/ { link = $2; } END { print link; }'`

usage () {
    echo "Usage: run.sh [options]"
    echo "Options:"
    echo "    --arch ARCH	Architecture name"
    echo "    --config NAME	Configuration name"
    echo "    --commit SHA	Commit of the Mono package"
    echo "    --build-url URL	URL of the build"
    echo "    --install-only	Only install the package"
    echo "    --benchmarks LIST	Only run specified benchmarks"
    echo "OSX options:"
    echo "    --pkg-file PATH	Mono installer .pkg file"
    echo "    --pkg-url URL	URL of Mono installer .pkg"
    echo "Debian options:"
    echo "    --snapshot-url URL"
    echo "			URL of a snapshot download directory"
    echo "    --deb-urls ASMURL BINURL"
    echo "			URLs of the .deb packages"
    echo "    --deb-common-url URL"
    echo "			URL of the common .deb package"
    exit $1
}

while [ "$1" != "" ]; do
    case $1 in
	--arch )		shift
				ARCH=$1
				;;
	--config )		shift
				CONFIG_NAME=$1
				;;
	--commit )		shift
				COMMIT=$1
				;;
	--build-url )		shift
				BUILD_URL=$1
				;;
	--install-only )	INSTALL_ONLY=true
				;;
	--benchmarks )		shift
				BENCHMARKS="--benchmarks $1"
				;;
        --pkg-file )   	        shift
                       		MONO_PKG=$1
                          	;;
        --pkg-url )            	shift
                           	MONO_PKG_URL=$1
                           	;;
	--snapshot-url )	shift
				SNAPSHOT_URL=$1
				;;
	--deb-urls )		shift
				DEB_ASM_URL=$1
				shift
				DEB_BIN_URL=$1
				;;
	--deb-common-url )	shift
				DEB_COMMON_URL=$1
				;;
        -h | --help )           usage 0
                                ;;
        * )                     usage 1
    esac
    shift
done

if [ "x$ARCH" = "x" ] ; then
    echo "Error: No architecture name given"
    exit 1
fi

if [ "x$CONFIG_NAME" = "x" ] ; then
    echo "Error: No configuration name given"
    exit 1
fi

TMP_DIR="/tmp/benchmarker.$RANDOM$RANDOM"
mkdir "$TMP_DIR"
if [ $? -ne 0 ] ; then
    echo "Error: Cannot create temporary directory $TMP_DIR"
    exit 1
fi

if [ "x$SNAPSHOT_URL" != "x" ] ; then
    BUILD_URL="$SNAPSHOT_URL"

    pushd "$TMP_DIR"

    lynx -dump -hiddenlinks=listonly -listonly "$SNAPSHOT_URL" | awk '$1 ~ /[0-9]+\./ { print $2 }' >"snapshot-links"
    if [ $? -ne 0 ] ; then
	echo "Error: Could not get links from snapshot URL"
	exit 1
    fi

    DEBIAN_TAR_URL=`grep '\.debian\.tar\.bz2' "snapshot-links"`
    if [ $? -ne 0 -o "x$DEBIAN_TAR_URL" = "x" ] ; then
	echo "Error: Could not get .debian.tar.bz2 link from snapshot URL"
	exit 1
    fi

    curl -o "$TMP_DIR/debian.tar.bz2" "$DEBIAN_TAR_URL"
    if [ $? -ne 0 ] ; then
	echo "Error: Could not fetch .debian.tar.bz2 from $DEBIAN_TAR_URL"
	exit 1
    fi


    tar -jxvf "debian.tar.bz2"
    if [ $? -ne 0 ] ; then
	echo "Error: Could not untar debian.tar.bz2"
	exit 1
    fi

    SHORT_COMMIT=`perl -n -e '/commit ID ([0-9a-f]+)/ && print $1' debian/changelog`
    if [ $? -ne 0 ] ; then
	echo "Error: Could not get commit ID from debian.tar.bz2"
	exit 1
    fi

    DEB_ASM_URL=`grep '-assemblies' "snapshot-links"`
    if [ $? -ne 0 -o "x$DEB_ASM_URL" = "x" ] ; then
	echo "Error: Could not get assemblies package link from snapshot URL"
	exit 1
    fi

    DEB_BIN_URL=`grep "$ARCH\.deb" "snapshot-links"`
    if [ $? -ne 0 -o "x$DEB_BIN_URL" = "x" ] ; then
	echo "Error: Could not get binary package link from snapshot URL"
	exit 1
    fi

    rm -rf "debian.tar.bz2" "debian" "snapshot-links"

    popd

    COMMIT=`curl "https://api.github.com/repos/mono/mono/commits/$SHORT_COMMIT" | jq -r '.sha'`

    echo "Commit $COMMIT"
    echo "assemblies $DEB_ASM_URL"
    echo "binary $DEB_BIN_URL"
fi

if [ "x$COMMIT" = "x" ] ; then
    echo "Error: No commit given"
    exit 1
fi

if [ "x$BUILD_URL" = "x" ] ; then
    echo "Error: No build URL given"
    exit 1
fi

if [ "x$DEB_COMMON_URL" = "x" ] ; then
    echo "Error: No URL for the common .deb package given"
    exit 1
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

ERROR=0

finish () {
    if [ "$OS" = "Darwin" ] ; then
	if [ $MOUNTED -ne 0 ] ; then
	    diskutil umount force "$VOLPATH"
	    if [ $? -ne 0 ] ; then
		echo "Error: Cannot unmount disk image"
		ERROR=1
	    fi
	fi
    fi

    if [ "$OS" = "Linux" ] ; then
        sudo /bin/rm -rf "$TMP_DIR"
        case "$ARCH" in
            amd64)
                cleanup_linux_amd64
                ;;
        esac
    else
	rm -rf "$TMP_DIR"
    fi
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

init_darwin () {
    VOLNAME=MonoInstallation$RANDOM
    VOLPATH=/Volumes/$VOLNAME

    MOUNTED=0

    if [ "x$MONO_PKG" = "x" -a "x$MONO_PKG_URL" = "x" ] ; then
	echo "Error: No package file or URL given"
	exit 1
    fi

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

    MONO_ROOT="$VOLPATH/Library/Frameworks/Mono.framework/Versions/$VERSION"
}

init_linux () {
    if [ "x$DEB_ASM_URL" = "x" -o "x$DEB_BIN_URL" = "x" -o "x$DEB_COMMON_URL" = "x" ] ; then
	error "Error: Debian package URLs not given"
    fi

    curl -o "$TMP_DIR/common.deb" "$DEB_COMMON_URL"
    if [ $? -ne 0 ] ; then
	error "Error: Could not download mono-snapshot-common package."
    fi

    curl -o "$TMP_DIR/assemblies.deb" "$DEB_ASM_URL"
    if [ $? -ne 0 ] ; then
	error "Error: Could not download mono-snapshot-assemblies package."
    fi

    curl -o "$TMP_DIR/mono.deb" "$DEB_BIN_URL"
    if [ $? -ne 0 ] ; then
	error "Error: Could not download mono-snapshot package."
    fi

    INSTALL_ROOT="$TMP_DIR/installation"

    mkdir -p "$INSTALL_ROOT/var/lib"
    if [ $? -ne 0 ] ; then
	error "Error: Could not create directory for dpkg."
    fi

    cp -a /var/lib/dpkg "$INSTALL_ROOT/var/lib/"

    sudo /usr/bin/dpkg --root="$INSTALL_ROOT" --unpack "$TMP_DIR/common.deb"
    if [ $? -ne 0 ] ; then
	error "Error: Could not install mono-snapshot-common package."
    fi

    sudo /usr/bin/dpkg --root="$INSTALL_ROOT" --unpack "$TMP_DIR/assemblies.deb"
    if [ $? -ne 0 ] ; then
	error "Error: Could not install mono-snapshot-assemblies package."
    fi

    sudo /usr/bin/dpkg --root="$INSTALL_ROOT" --unpack "$TMP_DIR/mono.deb"
    if [ $? -ne 0 ] ; then
	error "Error: Could not install mono-snapshot package."
    fi

    VERSION=`ls "$INSTALL_ROOT/opt"`
    if [ $? -ne 0 ] ; then
	error "Error: Nothing installed in /opt."
    fi

    if [ `echo "$VERSION" | wc -w` -ne 1 ] ; then
	error No unique Mono version in package
    fi

    echo Mono version is $VERSION

    MONO_ROOT="$INSTALL_ROOT/opt/$VERSION"

    # FIXME: Do we need this on OSX, too?
    sudo /bin/sed -i -e "s|/opt/|$INSTALL_ROOT/opt/|" "$MONO_ROOT/etc/mono/config"

    # Directory for shadow copies
    sudo /bin/rm -rf /tmp/nunit20
}

function setup_linux_amd64 {
    # disable intel turbo mode
    (echo 0 | sudo tee /sys/devices/system/cpu/cpufreq/boost) || error "only Intel CPUs supported"

    # force C0 state
    # cf. http://pm-blog.yarda.eu/2011/10/deeper-c-states-and-increased-latency.html
    # it's important to keep the file descriptor alive during the benchmark runs.
    # the old setting will be restored after closing the file descriptor.
    sudo -b bash -c "exec 3>/dev/cpu_dma_latency; echo -ne '\x00\x00\x00\x00' >&3; while sleep 60m; done"
}

function cleanup_linux_amd64 {
    sudo kill `sudo lsof -t /dev/cpu_dma_latency`
}

OS=`uname`
case "$OS" in
    Darwin )
        init_darwin
        ;;
    Linux )
        init_linux
        case "$ARCH" in
            amd64)
                setup_linux_amd64
                ;;
        esac
        ;;
    * )
        echo "Unsupported OS $OS"
        exit 1
        ;;
esac

MONO_PATH="$MONO_ROOT/bin/mono-sgen"

if [ "x$INSTALL_ONLY" = "xtrue" ] ; then
    echo "Mono is installed in $MONO_PATH"
    exit
fi

"$GLOBAL_MONO" "$COMPARE_EXE" $BENCHMARKS --commit "$COMMIT" --build-url "$BUILD_URL" --root "$MONO_ROOT" "$BENCHMARKER_ROOT/tests" "$BENCHMARKER_ROOT/benchmarks" "$BENCHMARKER_ROOT/machines" "$CONFIG_PATH"
if [ $? -ne 0 ] ; then
    error Error running benchmarks
fi

finish
