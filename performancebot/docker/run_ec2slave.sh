#!/bin/bash

set -e

if [ $# -ne 2 ]; then
    echo "./run_ec2slave.sh <masterhost> <slavepwd>"
    exit 1
fi

git clone --depth 1 https://github.com/xamarin/benchmarker/ /tmp/benchmarker
export PATH="/usr/lib/ccache:$PATH"
cd /tmp/benchmarker/performancebot && bash -x bootSlave.sh "$1" "$2"
