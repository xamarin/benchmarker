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

nuget restore tools.sln
xbuild /t:clean
rm -rf AndroidAgent/{bin,obj}

# build compare
xbuild /p:Configuration=Release /target:compare

# build xtcloghelper
xbuild /t:xtcloghelper /p:Configuration=Debug

OLDIFS=$IFS
IFS=','
for i in "Nexus-5_4.4.4",aba2bb7e,"--test-chunk" "Nexus-5_4.4.4-f36cc9c33f1a",f36cc9c33f1a,"--priority"; do
	set $i
	DEVICENAME=$1
	DEVICEID=$2
	XTCOPTS=$3

	RUNSETID=$(mono --debug ./compare.exe \
		--main-product mono $MONOCOMMITSHA \
		--secondary-product monodroid $MONODROIDCOMMITSHA \
		--build-url $BUILDURL \
		--create-run-set \
		--machine $DEVICENAME \
		--config-file ../configs/default.conf \
		| grep runSetId | grep -o -E '\d+')

	echo "runSetId: $RUNSETID"

	# build app + uitests
	(cd AndroidAgent && $XBUILDANDROID /p:Configuration=Release /verbosity:diagnostic /target:SignAndroidPackage )
	(cd AndroidAgent.UITests/ && $XBUILDANDROID /verbosity:diagnostic /p:Configuration=Release )

	XTCUPLOADLOG=$(mktemp /tmp/xtc-upload.XXXXXX)
	UITESTS=(./packages/Xamarin.UITest.*/tools/test-cloud.exe)
	UITEST="${UITESTS[${#UITESTS[@]} - 1]}" # select most recent version
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
	mono --debug xtcloghelper/bin/Debug/xtcloghelper.exe --push "$XTCJOBID" "$RUNSETID"
done

IFS=$OLDIFS
