#!/bin/bash

if [ $# -lt 1 ] ; then
    echo "Usage: plot.sh <base>"
    exit 1
fi

BASE="$1"

gnuplot "$BASE.gnu"
gs -q -r80x80 -g500x200 -sDEVICE=png16m -sOutputFile=out.png -dTextAlphaBits=4 -dGraphicsAlphaBits=4 out.eps -c quit - </dev/null
convert out.png -crop 406x154+54+0 "$BASE"_large.png
convert "$BASE"_large.png -resize "30%" "$BASE.png"
