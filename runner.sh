#!/bin/bash

. runner.conf

DIR=`pwd`
PREFIX="$DIR/installation"

highest_rev () {
    pushd "$REVISION_DIR" >/dev/null

    ls r* 2>/dev/null | cut -c 2- | sort -n | tail -1

    popd >/dev/null
}

REV=`highest_rev`

if [ "x$REV" = "x" ] ; then
    echo "No revision to process."
    exit
fi

REVFILE="$REVISION_DIR/r$REV"

echo "Processing revision $REV."

cd mono
svn update -r "$REV"
RESULT=$?
cd ..
if [ $RESULT -ne 0 ] ; then
    echo "SVN update of mono failed."
    exit 1
fi

cd mcs
svn update -r "$REV"
RESULT=$?
cd ..
if [ $RESULT -ne 0 ] ; then
    echo "SVN update of mcs failed."
    exit 1
fi

cd mono
./autogen.sh --prefix="$PREFIX" --with-moonlight=no
if [ $? -ne 0 ] ; then
    cd ..
    mv "$REVFILE" "$REVISION_DIR/broken/"
    echo "autogen.sh failed."
    exit 1
fi

make -j2
if [ $? -ne 0 ] ; then
    cd ..
    mv "$REVFILE" "$REVISION_DIR/broken/"
    echo "make failed."
    exit 1
fi

make install
RESULT=$?
cd ..
if [ $RESULT -ne 0 ] ; then
    mv "$REVFILE" "$REVISION_DIR/broken/"
    echo "make install failed."
    exit 1
fi

rm results/*

./speedtest.sh
if [ $? -ne 0 ] ; then
    mv "$REVFILE" "$REVISION_DIR/broken/"
    echo "speedtest failed."
    exit 1
fi

mkdir "$CONFIG_DIR/r$REV"
mv results/* "$CONFIG_DIR/r$REV/"

rm "$REVFILE"
