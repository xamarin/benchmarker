#!/bin/bash

gnuplot single.gnu
gs -q -r80x80 -g500x200 -sDEVICE=png16m -sOutputFile=out.png -dTextAlphaBits=4 -dGraphicsAlphaBits=4 out.eps -c quit - </dev/null
convert out.png -crop 406x154+54+0 single_large.png
convert single_large.png -resize "30%" single.png
