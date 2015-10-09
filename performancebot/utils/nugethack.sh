#!/bin/bash

set -x

# hack around nuget crashes (fixed in newer mono versions (>=4.3), but we won't
# upgrade the system mono anytime soon on our machines

export MONO_OPTIONS=--debug
for run in {1..20}; do
    timeout --foreground --kill-after=60 --signal=9 180 nuget restore tools.sln && break
done
