#!/bin/bash

set -x
set -e
set -o pipefail

PARAMSJSON="AndroidAgent.UITests/params.json"
XTCAPIKEY="../xtc-api-key"

function checkjsonfield()
{
    (cat "$PARAMSJSON" | jq -e '.'$1 > /dev/null) || (echo "file $PARAMSJSON must contain field $1" && exit 4)
}

if [ $# -ne 1 ]; then
    echo "./submit2xtc.sh <commit sha1>"
    exit 1
fi

if [ ! -f $XTCAPIKEY ]; then
    echo "$XTCAPIKEY file must exist"
    exit 2
fi

if [ ! -f $PARAMSJSON ]; then
    echo "$PARAMSJSON file must exist"
    exit 3
fi

# check if the json file has the required fields
checkjsonfield 'bmUsername'
checkjsonfield 'bmPassword'
checkjsonfield 'githubAPIKey'
checkjsonfield 'runSetId'

COMMITSHA="$1"

xbuild /p:Configuration=Release /target:compare

# generate run-set id for nexus5
RUNSETID=$(mono --debug ./compare.exe \
    --commit $COMMITSHA \
    --create-run-set \
    --machine "Nexus-5_4.4.4" -- ../tests/ ../benchmarks/ ../machines/ ../configs/default.conf \
    | grep runSetId | jq .runSetId)

echo "runSetId: $RUNSETID"

# insert new runSetId into JSON file
mv "$PARAMSJSON" "$PARAMSJSON"_template
cat "$PARAMSJSON"_template | jq 'to_entries | map(if .key == "runSetId" then . + {"value":'$RUNSETID'} else . end) | from_entries ' > $PARAMSJSON
rm "$PARAMSJSON"_template

# build app + uitests
(cd AndroidAgent && xbuild /p:Configuration=Release /target:SignAndroidPackage )
(cd AndroidAgent.UITests/ && xbuild /p:Configuration=Release )

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
    --user 'bernhard.urban@xamarin.com'

