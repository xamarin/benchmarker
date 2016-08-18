#!/bin/bash

set -x
set -e
set -o pipefail

if [ $# -ne 5 ]; then
    echo "usage: $0 <benchmarkerToolsDir> <mono-commit> <monodroid-commit> <build-url> <xbuild-android>"
    exit 1
fi

cd $1
MONOCOMMITSHA="$2"
MONODROIDCOMMITSHA="$3"
BUILDURL="$4"
XBUILDANDROID="$5"

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

if [ ! -f ./jq ]; then
	wget -O ./jq https://github.com/stedolan/jq/releases/download/jq-1.5/jq-osx-amd64
	chmod +x ./jq
fi

function checkjsonfield()
{
    (cat "$PARAMSJSON" | ./jq -e '.'$1 > /dev/null) || (echo "file $PARAMSJSON must contain field $1" && exit 4)
}

# check if the json file has the required fields
checkjsonfield 'githubAPIKey'
checkjsonfield 'httpAPITokens'
checkjsonfield 'machineName'
checkjsonfield 'runSetId'
checkjsonfield 'configName'

submitjob () {
	DEVICENAME=$1
	DEVICEID=$2
	XTCOPTS=$3
	RANDOMMOD=$4
	ENVFILE=$5
	CONFIGNAME=$6

	# on certain device groups we want to reduce pressure on the xtc queue
	if [ $(($RANDOM % $RANDOMMOD)) -ne 0 ]; then
		return
	fi

	RUNSETID=$(mono --debug ./compare.exe \
		--main-product mono $MONOCOMMITSHA \
		--secondary-product monodroid $MONODROIDCOMMITSHA \
		--build-url $BUILDURL \
		--create-run-set \
		--machine $DEVICENAME \
		--config-file ../configs/$CONFIGNAME.conf \
		| grep runSetId | grep -o -E '\d+')

	echo "runSetId: $RUNSETID"
	# insert new runSetId into JSON file
	PARAMTMP=$(mktemp /tmp/param_template.json.XXXXXX)
	mv "$PARAMSJSON" "$PARAMTMP"
	pwd
	cat "$PARAMTMP" | ./jq 'with_entries(if .key == "runSetId" then . + {"value":'$RUNSETID'} else . end) | with_entries(if .key == "machineName" then . + {"value":"'$DEVICENAME'"} else . end) | with_entries(if .key == "configName" then . + {"value":"'$CONFIGNAME'"} else . end)' > $PARAMSJSON
	rm -f "$PARAMTMP"

	(cd AndroidAgent && cp $ENVFILE env.txt)
	# build app + uitests
	if [ x$CONFIGNAME == xdefault-profiler ]; then
		(cd AndroidAgent && $XBUILDANDROID /p:AndroidEmbedProfilers=log /p:Configuration=Release /target:SignAndroidPackage )
	else
		(cd AndroidAgent && $XBUILDANDROID /p:Configuration=Release /target:SignAndroidPackage )
	fi
	(cd AndroidAgent.UITests/ && $XBUILDANDROID /p:Configuration=Release )

	XTCUPLOADLOG=$(mktemp /tmp/xtc-upload.XXXXXX)
	UITESTS=(./packages/Xamarin.UITest.*/tools/test-cloud.exe)
	UITEST="${UITESTS[${#UITESTS[@]} - 1]}" # select most recent version

	# workaround for "Test chunking failed: Unable to find NUnit 2 integration. Expected to find it in: /var/folders/hv/lh9y7pps1vbfsc737h_nl4mh0000gp/T/uitest/a-CE4F288E387B5B50427644AB2C0CCB5544D06478/nunit2/Xamarin.UITest.Integration.NUnit2.exe"
	# should be fixed in future UITest releases.
	rm -rf /var/folders/hv/lh9y7pps1vbfsc737h_nl4mh0000gp/T/uitest/a-CE4F288E387B5B50427644AB2C0CCB5544D06478/

	# submit to xtc
	mono \
		$UITEST \
		submit \
		./AndroidAgent/bin/Release/com.xamarin.benchmarkagent.apk \
		`cat $XTCAPIKEY` \
		--devices $DEVICEID \
		--async \
		$XTCOPTS \
		--app-name AndroidAgent \
		--assembly-dir ./AndroidAgent.UITests/bin/Release \
		--user 'bernhard.urban@xamarin.com' | tee $XTCUPLOADLOG

	XTCJOBID=$(grep -E -o '[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}' "$XTCUPLOADLOG")
	rm -f "$XTCUPLOADLOG"
	echo "submitted job has id $XTCJOBID"
	env
	echo "TODO: add xtc job id as log url to existing runset"
}

nuget restore tools.sln
xbuild /t:clean
rm -rf AndroidAgent/{bin,obj}

# build compare
xbuild /p:Configuration=Release /target:compare

OLDIFS=$IFS
IFS=','

# TODO, add this: "Nexus-5_4.4.4",aba2bb7e,"--test-chunk","1",env_conc.txt,default-conc
for i in "Nexus-5_4.4.4",aba2bb7e,"--test-chunk","1",env_default.txt,default "Nexus-5_4.4.4",aba2bb7e,"--test-chunk","1",env_profiler.txt,default-profiler "Nexus-5_4.4.4-f36cc9c33f1a",f36cc9c33f1a,"","2",env_default.txt,default; do
	set $i
	DEVICENAME=$1
	DEVICEID=$2
	XTCOPTS=$3
	RANDOMMOD=$4
	ENVFILE=$5
	CONFIGNAME=$6

	submitjob "$DEVICENAME" "$DEVICEID" "$XTCOPTS" "$RANDOMMOD" "$ENVFILE" "$CONFIGNAME"
done

IFS=$OLDIFS
