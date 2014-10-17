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

run_benchmark () {(
    benchmark_file="$1"
    if [ ! -f "$benchmark_file" ] ; then
	echo "Error: Benchmark file '$benchmark_file' doesn't exist.  Ignoring."
	return
    fi

    BENCHMARK_MEASURE=time

    . "$benchmark_file"

    if [ "x$BENCHMARK_NAME" = "x" ] ; then
	echo "Error: Benchmark file '$benchmark_name' doesn't specify a BENCHMARK_NAME.  Ignoring."
	return
    fi
    if [ "x$BENCHMARK_TESTDIR" = "x" ] ; then
	echo "Error: Benchmark file '$benchmark_name' doesn't specify a BENCHMARK_TESTDIR.  Ignoring."
	return
    fi
    if [ "x$BENCHMARK_CMDLINE" = "x" ] ; then
	echo "Error: Benchmark file '$benchmark_name' doesn't specify a BENCHMARK_CMDLINE.  Ignoring."
	return
    fi

    runtest "$BENCHMARK_NAME" "$BENCHMARK_TESTDIR" "$BENCHMARK_MEASURE" "${BENCHMARK_CMDLINE[@]}"
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

for fn in benchmarks/*.benchmark ; do
    run_benchmark $fn
done
