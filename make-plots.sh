#!/bin/bash

if [ $# -lt 1 ] ; then
    echo "Usage: make-plots.sh <dir>"
    exit 1
fi

DIR="$1"

for fn in "$DIR"/*.min.dat ; do
    bn=${fn%%.min.dat}
    bn=${bn##$DIR/}
    if [ ! "$bn" = "combined" ] ; then
	cp "$DIR/$bn.dat" plot/test.dat
	cp "$DIR/$bn.min.dat" plot/min.dat
	cp "$DIR/$bn.max.dat" plot/max.dat
	cd plot
	./plot.sh single
	cd ..
	cp plot/single.png "$DIR/$bn.png"
	cp plot/single_large.png "$DIR/$bn_large.png"
    fi
done

cp "$DIR/combined.dat" plot/test.dat
cd plot
./plot.sh combined
cd ..
cp plot/combined_large.png "$DIR/combined_large.png"
