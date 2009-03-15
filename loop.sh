#!/bin/bash

while true ; do
	./runner.sh
	./collect.pl
	sleep 10
done
