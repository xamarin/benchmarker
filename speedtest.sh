#!/bin/bash

. runner.conf

TIME="/Users/schani/Work/unix/mytime/mytime"
DIR=`pwd`
MONO="$DIR/installation/bin/mono-sgen"
OUTDIR="$DIR/results"
TMPPREFIX="/tmp/speedtest$$"

grepscimark () {
    grep Composite "$TMPPREFIX.out" | awk '{ print $3 }' >"$OUTDIR/scimark.times"
    grep FFT "$TMPPREFIX.out" | awk '{ print $3 }' >"$OUTDIR/scimark-fft.times"
    grep SOR "$TMPPREFIX.out" | awk '{ print $3 }' >"$OUTDIR/scimark-sor.times"
    grep Monte "$TMPPREFIX.out" | awk '{ print $4 }' >"$OUTDIR/scimark-montecarlo.times"
    grep Sparse "$TMPPREFIX.out" | awk '{ print $4 }' >"$OUTDIR/scimark-matmult.times"
    grep LU "$TMPPREFIX.out" | awk '{ print $3 }' >"$OUTDIR/scimark-lu.times"
}

runtest () {
    echo "$1"

    pushd "tests/$2" >/dev/null

    measure="$3"

    #the first run is not timed
    $MONO --stats $4 $5 $6 $7 $8 $9 >"$TMPPREFIX.stats" 2>/dev/null
    if [ $? -ne 0 ] ; then
	popd >/dev/null
	return
    fi
    grep -a 'Native code size:' "$TMPPREFIX.stats" | awk '{ print $4 }' >"$OUTDIR/$1.size"

    rm -f "$TMPPREFIX.times" "$TMPPREFIX.out"
    i=1
    while [ $i -le $COUNT ] ; do
	if [ "$measure" = time ] ; then
	    $TIME "$TMPPREFIX.times" $MONO $4 $5 $6 $7 $8 $9 >/dev/null 2>&1
	else
	    $MONO $4 $5 $6 $7 $8 $9 >>"$TMPPREFIX.out"
	fi
	i=$(($i + 1))
    done

    popd >/dev/null

    if [ "$measure" = time ] ; then
	cp "$TMPPREFIX.times" "$OUTDIR/$1.times"
	rm "$TMPPREFIX.times"
    else
	$measure
    fi

    echo "Size"
    cat "$OUTDIR/$1.size"
    echo "Times"
    cat "$OUTDIR/$1.times"
}

#runtest myfib small time myfib.exe
#runtest monofib small time fib.exe 42
#runtest scimark scimark grepscimark scimark.exe
#runtest gmcs gmcs time gmcs.exe -define:NET_1_1 -out:mcs.exe @mcs.exe.sources cs-parser.cs
runtest fsharp f-sharp-2.0 time fsc.exe GeneralTest1.fs
runtest ipy IronPython-2.0B2 time ipy.exe pystone.py 500000
runtest binarytree shootout time binarytree.exe 20
runtest n-body shootout time n-body.exe 50000000
runtest graph4 graph time graph4.exe
runtest graph8 graph time graph8.exe
#runtest mandelbrot shootout time mandelbrot.exe 6400
#runtest compileswf compile time --compile-all System.Windows.Forms.dll
