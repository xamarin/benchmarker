#!/bin/bash

set -x
set -e
set -o pipefail

if [ $# -ne 4 ]; then
    echo "usage: $0 <benchmarkerToolsDir> <commit> <build-url> <xbuild-android>"
    exit 1
fi

cd $1
COMMITSHA="$2"
BUILDURL="$3"
XBUILDANDROID="$4"

PARAMSJSON="AndroidAgent.UITests/params.json"
XTCAPIKEY="../xtc-api-key"

if [ ! -f $XTCAPIKEY ]; then
    echo "$XTCAPIKEY file must exist"
    exit 2
fi

if [ ! -f $PARAMSJSON ]; then
    echo "$PARAMSJSON file must exist"
    exit 3
fi

nuget restore tools.sln
xbuild /t:clean
rm -rf AndroidAgent/{bin,obj}

# build compare
xbuild /p:Configuration=Release /target:compare

# build xtcloghelper
xbuild /t:xtcloghelper /p:Configuration=Debug

# generate run-set id for nexus5
RUNSETID=$(mono --debug ./compare.exe \
    --commit $COMMITSHA \
    --build-url $BUILDURL \
    --create-run-set \
    --machine "Nexus-5_4.4.4" -- ../tests/ ../benchmarks/ ../machines/ ../configs/default.conf \
    | grep runSetId | grep -o -E '\d+')

echo "runSetId: $RUNSETID"

# build app + uitests
(cd AndroidAgent && $XBUILDANDROID /p:Configuration=Release /target:SignAndroidPackage )
(cd AndroidAgent.UITests/ && $XBUILDANDROID /p:Configuration=Release )

XTCUPLOADLOG=$(mktemp /tmp/xtc-upload.XXXXXX)
UITESTS=(./packages/Xamarin.UITest.*/tools/test-cloud.exe)
UITEST="${UITESTS[${#UITESTS[@]} - 1]}" # select most recent version
# submit to xtc
mono \
    $UITEST \
    submit \
    ./AndroidAgent/bin/Release/com.xamarin.benchmarkagent.apk \
    `cat $XTCAPIKEY` \
    --devices aba2bb7e \
    --async \
    --fixture-chunk \
    --fixture AndroidAgent \
    --app-name AndroidAgent \
    --assembly-dir ./AndroidAgent.UITests/bin/Release \
    --user 'bernhard.urban@xamarin.com' | tee $XTCUPLOADLOG

XTCJOBID=$(grep -E -o '[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}' "$XTCUPLOADLOG")
rm -f "$XTCUPLOADLOG"
echo "submitted job has id $XTCJOBID"
mono --debug xtcloghelper/bin/Debug/xtcloghelper.exe --push "$XTCJOBID" "$RUNSETID"

