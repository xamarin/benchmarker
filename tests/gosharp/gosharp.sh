#!/usr/bin/env bash

# Convert Go tests into their CIL form, where each test runs below $TIME_LIMIT, the tests are placed into the "cil" directory.
# The code is ugly, plenty of error checks is missing, but it does its job.
# Note that the script was only intended to work on OS X with the Go1 suite, however, adapting it to work on other *nix systems and test suites should be a fairly easy and straightforward process.
# On OS X, it depends on some additional command line tools, such as Go and GNU coreutils that are not shipped with the system, and need to be installed additionally, e.g. via the Brew package manager.

# TODO
# Make sure this works both on OSX and Linux (and MinGW?), without the need of any additional software.
# Support other Go benchmarks.

#set -e
#set -vx

TIME_LIMIT=20

TESTPROJ=gosharp
TESTDIR="$PWD/go/src/$TESTPROJ"
CILDIR="$PWD/cil"
GOPATH="$PWD/go"
GOSRC="$GOPATH/src"

# Create infrastructure
if [ ! -e go ]; then
    mkdir -p "$GOPATH/src"
fi
if [ ! -e tardisgo ]; then
    TARDIS_PATH="github.com/tardisgo/tardisgo"
    go get "$TARDIS_PATH"
fi
if [ ! -e "$CILDIR" ]; then
    mkdir -p "$CILDIR"
fi

# Paranoia check for $TESTDIR, since we will be calling rm -rf on it
if [ ${#TESTDIR} -le 5 ]; then
    echo "A dodgy error has occurred."
    exit 1
fi

# Prepare the directory for building CIL
rm -rf "$TESTDIR"

# Golang's Go1 suite
# It is a special case since it has its own hard-coded driver (which is sadly not very
# straightforward to adjust for this task) and the tests do not abide by the standard Go path
# structure (oh irony)
if [ ! -e golang ]; then
    git clone https://github.com/golang/go golang
fi

# Fill the Go1 package with test files
# Rename files so that our non-test run would notice them
GO1SRC="$PWD/golang/test/bench/go1"

# Create a separate executable for each test in the suite
for BENCH_FILE_PATH in "$GO1SRC"/*; do
    BENCH_FILE="$(basename $BENCH_FILE_PATH)"
    if [ -d "$BENCH_FILE" ]; then rm "$BENCH_FILE"; fi
    for BENCH_NAME in $(gsed -nE 's/^\W*func (Benchmark[^a-z]\w*).*/\1/p' "$BENCH_FILE_PATH"); do
	ITER_COUNT=1
	TIME=0
	while true; do # > 10 seconds

	    echo -n "Generating test for ${BENCH_NAME} located at ${BENCH_FILE} with $ITER_COUNT iteration(s)... "

	    CILNAME="$CILDIR/$(sed 's/^Benchmark//' <<<$BENCH_NAME.exe)"
	    
	    mkdir "$TESTDIR"
	    cat <<< "package main

import (
       \"go1\"
       \"testing\"
       \"fmt\"
       \"os\"
)

func err() {
    fmt.Println(\"Usage:\\n$BENCH_NAME.exe N\\nwhere N is the number of iterations\")
    os.Exit(1)
}

func main() {
    fmt.Println(\"$BENCH_NAME: a Go test from $BENCH_FILE compiled into CIL. (iteration count N: $ITER_COUNT)\")

    b := testing.B{N: $ITER_COUNT}
    go1.${BENCH_NAME}(&b)
}
" > "$TESTDIR"/gosharp.go

	    # Recreate go1 dir
	    GO1PATH="$GOSRC/go1"
	    rm -rf "$GO1PATH"
	    mkdir "$GO1PATH"

	    # Fill with appropriate tests
	    cp "$BENCH_FILE_PATH" "$GO1PATH/$(sed 's/_test.go/_extest.go/' <<<$BENCH_FILE)"
	    
	    # Manually copy dependencides. DCE in Tardis is nonexistent(?), so can't keep the whole fat package...
	    # Otherwise Haxe will chug for good 10 minutes, and then quit with an "unknown error".
	    for t in gob gzip json template; do
		if [ "$BENCH_FILE" == "${t}_test.go" ]; then
		    for f in json jsondata; do
			cp "${GO1SRC}/${f}_test.go" "${GO1PATH}/${f}_extest.go"
		    done
		fi
	    done
	    if [ "$BENCH_FILE" == "revcomp_test.go" ]; then
		cp "${GO1SRC}/fasta_test.go" "${GO1PATH}/fasta_extest.go"
	    fi
	    if [ "$BENCH_FILE" == "parser_test.go" ]; then
		cp "${GO1SRC}/parserdata_test.go" "${GO1PATH}/parserdata_extest.go"
	    fi
	    
	    if false; then
		# Test run with the standard Go
		go build -o "./go/bin/$BENCH_NAME" gosharp
		"./go/bin/$BENCH_NAME" 1
	    else
		# Actually convert to CIL
		pushd "$TESTDIR"
		"$GOPATH/bin/tardisgo" gosharp # gosharp.go
		haxe -main tardis.Go -cp tardis -dce full -D uselocalfunctions,no-compilation -cs tardis/go.cs
		pushd ./tardis/go.cs
		# Replacing public methods with internal could give some gains(?), but it's not as straightforward as this. TODO?
		# find src -type f -exec gsed -Ei '/Equals|GetHashCode|ToString|Message/!s/public/internal/' {} \;
		xbuild /p:TargetFrameworkVersion="v4.0" /p:Configuration=Release Go.csproj /p:AssemblyName="$BENCH_NAME"
		popd
		cp ./tardis/go.cs/bin/Release/"$BENCH_NAME".exe "$CILNAME"
		# exit
		rm -rf "$TESTDIR"
		popd
	    fi

	    # Test run application, measure its execution time
	    # TODO: need additional handling for times longer than 59 seconds, since this implementation will just discard the minute part
	    # TODO: The lack of error checks in this segment is especially alarming.
	    RAW_TIMES="$(/usr/bin/env time -p gtimeout $(((TIME_LIMIT+2))) mono $CILNAME 2>&1)"
	    echo "$RAW_TIMES"
	    TIME="$(sed -nE 's/^user.*[^0-9]([0-9]+)\.([0-9]+)$/\1/p' <<< "$RAW_TIMES")"

	    CILNAME_="${CILNAME}_" # Backup of the previous iteration
	    
	    # Analyze the result, make appropriate adjustements.
	    if [ "z$TIME" == "z" ]; then
		TIME=9001	# timed out, supposedly
	    fi
	    if [ $TIME -gt $TIME_LIMIT ]; then
		if [ -a "$CILNAME_" ]; then
		    mv "$CILNAME_" "$CILNAME" # restore the appropriate backup
		else
		    mv "$CILNAME" "${CILNAME}.bad" # Couldn't find the proper iteration counter, marking as bad
		fi
		break
	    else
		cp "$CILNAME" "$CILNAME_"
		echo "t($ITER_COUNT) = $TIME <= $TIME_LIMIT, multiplying by two"
		ITER_COUNT=$((($ITER_COUNT*2)))
	    fi
	done
    done
done

