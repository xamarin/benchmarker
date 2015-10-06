#!/bin/bash

set -x
set -e
set -o pipefail

if [ $# -ne 3 ]; then
    echo "usage: $0 <benchmarkerToolsDir> <commit> <xbuild-android>"
    exit 1
fi

cd $1
COMMITSHA="$2"
XBUILDANDROID="$3"

PARAMSJSON="AndroidAgent.UITests/params.json"
XTCAPIKEY="../xtc-api-key"

function checkjsonfield()
{
    (cat "$PARAMSJSON" | jq -e '.'$1 > /dev/null) || (echo "file $PARAMSJSON must contain field $1" && exit 4)
}

if [ ! -f $XTCAPIKEY ]; then
    echo "$XTCAPIKEY file must exist"
    exit 2
fi

if [ ! -f $PARAMSJSON ]; then
    echo "$PARAMSJSON file must exist"
    exit 3
fi

# check if the json file has the required fields
checkjsonfield 'githubAPIKey'
checkjsonfield 'runSetId'

nuget restore tools.sln
xbuild /t:clean
rm -rf AndroidAgent/{bin,obj}
xbuild /p:Configuration=Release /target:compare

# generate run-set id for nexus5
RUNSETID=$(mono --debug ./compare.exe \
    --commit $COMMITSHA \
    --create-run-set \
    --machine "Nexus-5_4.4.4" -- ../tests/ ../benchmarks/ ../machines/ ../configs/default.conf \
    | grep runSetId | jq .runSetId)

echo "runSetId: $RUNSETID"

# insert new runSetId into JSON file
PARAMTMP=$(mktemp /tmp/param_template.json.XXXXXX)
mv "$PARAMSJSON" "$PARAMTMP"
cat "$PARAMTMP" | jq 'to_entries | map(if .key == "runSetId" then . + {"value":'$RUNSETID'} else . end) | from_entries ' > $PARAMSJSON
rm -f "$PARAMTMP"

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
    --test-chunk \
    --fixture AndroidAgent \
    --app-name AndroidAgent \
    --assembly-dir ./AndroidAgent.UITests/bin/Release \
    --user 'bernhard.urban@xamarin.com' | tee $XTCUPLOADLOG

XTCJOBID=$(grep -E -o '[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}' "$XTCUPLOADLOG")
rm -f "$XTCUPLOADLOG"
echo "submitted job has id $XTCJOBID"

