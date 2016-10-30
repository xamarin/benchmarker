#!/bin/bash

set -x
set -e
set -o pipefail

if [ $# -ne 2 ]; then
    echo "usage: $0 <mono-git-rev> <targetdir>"
    exit 1
fi

GITREV=$1
TARGETDIR=$2
TARGETFILENAME="$TARGETDIR/sgen-grep-binprot-$GITREV"

if [ -f $TARGETFILENAME ]; then
    echo "$TARGETFILENAME already in place"
    exit 0
fi

WORKDIR=$(mktemp -d /tmp/mono-build.XXXXXX)
cd $WORKDIR

git clone -b binprot-par https://github.com/lewurm/mono
cd mono
git reset --hard $GITREV

./autogen.sh
CFLAGS='-g -O0' ./configure

make -C eglib/
make -C tools/sgen/

cp tools/sgen/sgen-grep-binprot $TARGETFILENAME

cd && rm -rf $WORKDIR
