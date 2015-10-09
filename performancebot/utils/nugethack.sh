#!/bin/bash

set -e
set -x

# hack around nuget crashes (fixed in newer mono versions (>=4.3), but we won't
# upgrade the system mono anytime soon on our machines

for run in {1..20}; do
    MONO_OPTIONS=--debug nuget restore tools.sln & pid=$!
    sleep 60 && (kill -0 $pid && echo trying to kill && kill -9 $pid) || break
done
