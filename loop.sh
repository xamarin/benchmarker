#!/bin/bash

. runner.conf

TMPFILE="/tmp/loop$$"

while true ; do
	HIGHEST_REV=`( (cd revisions ; ls r*) ; (cd revisions/broken ; ls r*) ; (cd "$CONFIG_DIR" ; ls -d r*) ) | grep '^r[0-9]\+$' | cut -c 2- | sort -n | tail -1`
	NEXT_REV=$(($HIGHEST_REV+1))
	echo next rev is $NEXT_REV
	(cd mono ; svn log -r$NEXT_REV:HEAD) | grep -e '^r[0-9]\+ \+|' | awk '{ print $1 }' | (cd revisions ; xargs -r touch)
	rm -rf mcs/tools/sqlmetal
	rm -rf mono/mono/os
	rm -f "$TMPFILE"
	./runner.sh "$TMPFILE"
	if [ -f "$TMPFILE" ] ; then
	    REV=`cat "$TMPFILE"`
	    ./collect.pl
	    scp -Cr configs/amd64/r$REV mprobst@www.go-mono.com:/var/www/mono-website/go-mono/performance/amd64/
	    scp -Cr configs/amd64/*.html mprobst@www.go-mono.com:/var/www/mono-website/go-mono/performance/amd64/
	    scp -Cr configs/amd64/*.png mprobst@www.go-mono.com:/var/www/mono-website/go-mono/performance/amd64/
	else
	    sleep 60
	fi
done
