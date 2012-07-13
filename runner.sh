#!/bin/bash

REVISION="$1"
shift

if [ "x$REVISION" = "x" ] ; then
    echo "Usage: runner.sh <revision> <config-file> ..."
    exit 1
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

    if [ ! -d "$RESULTS_DIR" ] ; then
	echo "Error: results directory '$RESULTS_DIR' does not exist."
	exit 1
    fi

    ./speedtest.sh "$config"
    if [ $? -ne 0 ] ; then
	exit 1
    fi

    if [ ! -d "$RESULTS_DIR/$CONFIG_NAME" ] ; then
	mkdir "$RESULTS_DIR/$CONFIG_NAME"
    fi

    mkdir "$RESULTS_DIR/$CONFIG_NAME/$REVISION"

    mv "$OUTDIR/"*.times "$OUTDIR/"*.size "$RESULTS_DIR/$CONFIG_NAME/$REVISION/"
done
