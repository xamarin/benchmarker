#!/bin/bash

COUNT=3
TIME="/usr/bin/time -f %e"
MONO="/home/schani/Work/novell/trunk/monosvn/mono/mini/mono"
DIR=`pwd`
OUTDIR="$DIR/results"
TMPPREFIX="/tmp/speedtest$$"

grepcomposite () {
    grep Composite | awk '{ print $3 }'
}

runtest () {
    echo "$1"

    pushd "$2" >/dev/null

    measure="$3"

    #the first run is not timed
    $MONO --stats $4 $5 $6 $7 $8 $9 >"$TMPPREFIX.stats" 2>/dev/null
    if [ $? -ne 0 ] ; then
	popd >/dev/null
	return
    fi
    grep -a 'Native code size:' "$TMPPREFIX.stats" | awk '{ print $4 }' >"$OUTDIR/$1.size"

    rm -f "$TMPPREFIX.times"
    i=1
    while [ $i -le $COUNT ] ; do
	if [ "$measure" == time ] ; then
	    $TIME --append --output="$TMPPREFIX.times" $MONO $4 $5 $6 $7 $8 $9 >/dev/null 2>&1
	else
	    $MONO $4 $5 $6 $7 $8 $9 | $measure >>"$TMPPREFIX.times"
	fi
	i=$(($i + 1))
    done

    popd >/dev/null

    cp "$TMPPREFIX.times" "$OUTDIR/$1.times"
    #awk -f avgdev.awk </tmp/speedtest.out >"$OUTDIR/$1.avgdev"
    rm "$TMPPREFIX.times"

    echo "Size"
    cat "$OUTDIR/$1.size"
    echo "Times"
    cat "$OUTDIR/$1.times"
}

runtest myfib small time myfib.exe
runtest monofib small time fib.exe 42
runtest scimark scimark grepcomposite scimark.exe
#runtest gmcs gmcs time gmcs.exe -define:NET_1_1 -out:mcs.exe @mcs.exe.sources cs-parser.cs
runtest fsharp f-sharp-2.0 time fsc.exe GeneralTest1.fs
runtest ipy IronPython-2.0B2 time ipy.exe pystone.py 200000
runtest binarytree shootout time binarytree.exe 20
runtest n-body shootout time n-body.exe 50000000
runtest mandelbrot shootout time mandelbrot.exe 6400
