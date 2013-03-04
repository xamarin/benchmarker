#!/bin/bash

usage () {
    echo "Usage: runner.sh [-c commit-sha1] <revision> <config-file> ..."
    exit $1
}

SHA=""

while getopts hc: OPT; do
    case "$OPT" in
	h)
	    usage 0
	    ;;
	c)
	    SHA="$OPTARG"
	    ;;
	\?)
	    usage 1
	    ;;
    esac
done

shift `expr $OPTIND - 1`

REVISION="$1"
shift

if [ "x$REVISION" = "x" ] ; then
    usage 1
fi

DIR=`pwd`
OUTDIR="$DIR/results"

for config in "$@" ; do
    echo "*** $config"
    if [ ! -f "$config" ] ; then
	echo "Error: config file '$config' does not exist."
	exit 1
    fi

    . "$config"

    mkdir -p "$RESULTS_DIR"

    ./speedtest.sh "$config"
    if [ $? -ne 0 ] ; then
	exit 1
    fi

    if [ ! -d "$RESULTS_DIR/$CONFIG_NAME" ] ; then
	mkdir "$RESULTS_DIR/$CONFIG_NAME"
    fi

    mkdir "$RESULTS_DIR/$CONFIG_NAME/r$REVISION"
    if [ "x$SHA" != "x" ] ; then
	echo "$SHA" >"$RESULTS_DIR/$CONFIG_NAME/r$REVISION/sha1"
    fi

    mv "$OUTDIR/"*.times "$OUTDIR/"*.size "$OUTDIR/"*.pauses "$RESULTS_DIR/$CONFIG_NAME/r$REVISION/"
done
