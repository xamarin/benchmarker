#!/bin/bash

if [ "x$BENCH_WALL_CLOCK" = x ] ; then
    BENCH_WALL_CLOCK=yes
fi
if [ "x$BENCH_PAUSE_TIME" = x ] ; then
    BENCH_PAUSE_TIME=no
fi

if [ "x$1" = "x" ] ; then
    echo "Usage: speedtest.sh <conf-file>"
    exit 1
fi

if [ ! -f "$1" ] ; then
    echo "Error: Config file '$1' does not exist."
    exit 1
fi

DIR=`pwd`

mono_env () {
    true
}

benchmark_env () {
    true
}

. "$1"

mono_env

TIME="$DIR/mytime/mytime"
OUTDIR="$DIR/results"
TMPPREFIX="/tmp/speedtest$$"
TIMEOUT=200

grepscimark () {
    grep FFT "$TMPPREFIX.out" | awk '{ print 10000.0 / $3 }' >"$OUTDIR/scimark-fft.times"
    grep SOR "$TMPPREFIX.out" | awk '{ print 10000.0 / $3 }' >"$OUTDIR/scimark-sor.times"
    grep Monte "$TMPPREFIX.out" | awk '{ print 10000.0 / $4 }' >"$OUTDIR/scimark-montecarlo.times"
    grep Sparse "$TMPPREFIX.out" | awk '{ print 10000.0 / $4 }' >"$OUTDIR/scimark-matmult.times"
    grep LU "$TMPPREFIX.out" | awk '{ print 10000.0 / $3 }' >"$OUTDIR/scimark-lu.times"
}

grepironjs () {
    grep -A1 -e 'Whole Suite' "$TMPPREFIX.out" | grep 'Score' | awk '{ print $2 }' | sed 's/ms//' >"$TMPPREFIX.ironjs"
    head -1 "$TMPPREFIX.ironjs" >"$OUTDIR/ironjs-sunspider.times"
    tail -1 "$TMPPREFIX.ironjs" | awk '{ print 10000.0 / $1 }' >"$OUTDIR/ironjs-v8.times"
}

runtest () {(
    name="$1"
    testdir="$2"
    measure="$3"

    echo "$name"

    benchmark_env "$name"

    pushd "tests/$testdir" >/dev/null

    shift; shift; shift

    #the stats run is not timed
    $TIME /dev/null "$TIMEOUT" "$MONO" "${MONO_OPTIONS[@]}" --stats "$@" >"$TMPPREFIX.stats" 2>/dev/null
    if [ $? -ne 0 ] ; then
	echo "Error"
	popd >/dev/null
	return
    fi
    cp "$TMPPREFIX.stats" "$OUTDIR/$name.stats"

    if [ "$BENCH_WALL_CLOCK" = yes ] ; then
	rm -f "$TMPPREFIX.times" "$TMPPREFIX.out"
	i=1
	while [ $i -le $COUNT ] ; do
	    if [ "$measure" = time ] ; then
		$TIME "$TMPPREFIX.times" "$TIMEOUT" "$MONO" "${MONO_OPTIONS[@]}" "$@" >/dev/null 2>&1
	    else
		$TIME /dev/null "$TIMEOUT" "$MONO" "${MONO_OPTIONS[@]}" "$@" >>"$TMPPREFIX.out"
	    fi
	    if [ $? -ne 0 ] ; then
		echo "Error"
		popd >/dev/null
		return
	    fi
	    i=$(($i + 1))
	done

	if [ "$measure" = time ] ; then
	    cp "$TMPPREFIX.times" "$OUTDIR/$name.times"
	    rm "$TMPPREFIX.times"
	else
	    $measure
	fi

	echo "Times"
	cat "$OUTDIR/$name.times"
    fi

    if [ "$BENCH_PAUSE_TIME" = yes ] ; then
	echo "benching pause times"

	if [ "x$PAUSE_COUNT" = "x" ] ; then
	    PAUSE_COUNT=1
	fi

	rm -f "$TMPPREFIX.pauses"
	i=1
	while [ $i -le $PAUSE_COUNT ] ; do
	    echo "*** run $i" >>"$TMPPREFIX.pauses"
	    if sudo dtrace -l -P 'mono$target' -c "$MONO" | grep gc-concurrent-update-finish-begin >/dev/null ; then
		SCRIPT='BEGIN { stt = timestamp; } mono$target:::gc-world-stop-begin { printf ("\nstop %d\n", (timestamp - stt)/1000); } mono$target:::gc-concurrent-start-begin { printf ("\nconcurrent %d\n", (timestamp - stt)/1000); } mono$target:::gc-concurrent-update-finish-begin { printf ("\nconcurrent %d\n", (timestamp - stt)/1000); } mono$target:::gc-world-restart-end { printf ("\nrestart %d %d\n", (timestamp - stt)/1000, arg0); }'
	    else
		SCRIPT='BEGIN { stt = timestamp; } mono$target:::gc-world-stop-begin { ts = timestamp; concurrent = 0; } mono$target:::gc-world-restart-end { printf ("\npause-time %d %d %d %d\n", 1, 0, (timestamp - ts)/1000, (ts - stt)/1000); }'
	    fi
	    #sudo MONO_GC_PARAMS="$MONO_GC_PARAMS" dtrace -q -c "$MONO $4 $5 $6 $7 $8 $9" -n "$SCRIPT" >>"$TMPPREFIX.pauses"
	    MONO_GC_PARAMS="$MONO_GC_PARAMS" "$MONO" "${MONO_OPTIONS[@]}" "$@" >>"$TMPPREFIX.pauses"
	    if [ $? -ne 0 ] ; then
		echo "Error"
		popd >/dev/null
		return
	    fi
	    i=$(($i + 1))
	done

	cp "$TMPPREFIX.pauses" "$OUTDIR/$name.pauses"
	rm "$TMPPREFIX.pauses"
    fi

    popd >/dev/null
)}

if [ ! -f "$TIME" ] ; then
    echo Building mytime
    pushd mytime >/dev/null
    make
    if [ $? -ne 0 ] ; then
	popd >/dev/null
	echo "Build failed"
	exit 1
    fi
    popd >/dev/null
fi

if [ ! -d "$OUTDIR" ] ; then
    mkdir "$OUTDIR"
fi

if [ ! -f "$MONO" ] ; then
    echo "Error: Missing mono.  Expected to be $MONO."
    exit 1
fi

#runtest myfib small time myfib.exe
#runtest monofib small time fib.exe 42
runtest scimark scimark grepscimark scimark.exe
#runtest gmcs gmcs time gmcs.exe -define:NET_1_1 -out:mcs.exe @mcs.exe.sources cs-parser.cs

runtest ironjs IronJS grepironjs ijs.exe .

runtest euler csgrande/Euler/Euler/bin/Debug time Euler.exe
runtest grandetracer csgrande/GrandeTracer/GrandeTracer/bin/Debug time GrandeTracer.exe
runtest bh csolden/BH/BH/bin/Debug time BH.exe -b 700 -s 1000
runtest bisort csolden/BiSort/BiSort/bin/Debug time BiSort.exe -s 3000000
runtest health csolden/Health/Health/bin/Debug time Health.exe -l 10 -t 40
runtest perimeter csolden/Perimeter/Perimeter/bin/Debug time Perimeter.exe -l 17
runtest specraytracer csspec/SpecRaytracer/SpecRaytracer/bin/Debug time SpecRaytracer.exe 200 20000 ../time-test.model
runtest db csspec/DB/DB/bin/Debug time DB.exe ../input/db6 ../input/scr6
runtest ahcbench zorn/ahcbench time ahcbench.exe input3.cs
runtest lcscbench zorn/lcscbench time lcscbench.exe input3.cs
runtest sharpsatbench zorn/SharpSATbench time SharpSATbench.exe input3.cnf

runtest fsharp f-sharp-2.0 time fsc.exe GeneralTest1.fs
runtest ipy IronPython-2.0B2 time ipy.exe pystone.py 500000

runtest roslyn roslyn/corlib time ../roslyn/csc.exe /codepage:65001 -unsafe -nostdlib -nowarn:612,618 -d:INSIDE_CORLIB -d:LIBC  -d:NET_1_1 -d:NET_2_0 -d:NET_3_0 -d:NET_3_5 -d:NET_4_0 -d:NET_4_5 -nowarn:1699 -nostdlib /noconfig -resource:resources/collation.core.bin -resource:resources/collation.tailoring.bin -resource:resources/collation.cjkCHS.bin -resource:resources/collation.cjkCHT.bin -resource:resources/collation.cjkJA.bin -resource:resources/collation.cjkKO.bin -resource:resources/collation.cjkKOlv2.bin -target:library -out:mscorlib-out.dll "@corlib.dll.sources"

runtest binarytree shootout time binarytree.exe 19
runtest except shootout time except.exe 10000000
runtest hash shootout time hash.exe 10000000
runtest lists shootout time lists.exe 30000
runtest mandelbrot shootout time mandelbrot.exe 6400
runtest message shootout time message.exe 1000000
runtest n-body shootout time n-body.exe 50000000
runtest objinst shootout time objinst.exe 400000000
runtest raytracer2 shootout time raytracer.csharp-2.exe 250
runtest raytracer3 shootout time raytracer.csharp-3.exe 600
runtest strcat shootout time strcat.exe 80000000

runtest graph4 graph time graph4.exe
runtest graph8 graph time graph8.exe

runtest sharpchess SharpChess time Program.exe TestPosition.sharpchess 7

#runtest compileswf compile time --compile-all System.Windows.Forms.dll
