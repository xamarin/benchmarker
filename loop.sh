#!/bin/bash

. runner.conf

while true ; do
	HIGHEST_REV=`( (cd revisions ; ls r*) ; (cd revisions/broken ; ls r*) ; (cd "$CONFIG_DIR" ; ls -d r*) ) | grep '^r[0-9]\+$' | cut -c 2- | sort -n | tail -1`
	NEXT_REV=$(($HIGHEST_REV+1))
	echo next rev is $NEXT_REV
	(cd mono ; svn log -r$NEXT_REV:HEAD) | grep -e '^r[0-9]\+ \+|' | awk '{ print $1 }' | (cd revisions ; xargs touch)
	./runner.sh
	./collect.pl
	sleep 10
done
