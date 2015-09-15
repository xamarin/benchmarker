#!/bin/bash

set -x
set -e
set -o pipefail

if [ $# -ne 1 ]; then
    echo "./submit2xtc.sh <commit sha1>"
    exit 1
fi

COMMITSHA="$1"

# generate run-set id for nexus5
RUNSETID=$(mono --debug ./compare.exe \
    --commit $COMMITSHA \
    --create-run-set \
    --machine "Nexus-5_4.4.4" -- ../tests/ ../benchmarks/ ../machines/ ../configs/default.conf \
    | grep runSetId | jq .runSetId)

echo "runSetId: $RUNSETID"

# TODO: fixup json file

# TODO: build app + uitests
(cd AndroidAgent && xbuild /p:Configuration=Release /target:SignAndroidPackage )
(cd AndroidAgent.UITests/ && xbuild /p:Configuration=Release )


# TODO: submit to xtc
