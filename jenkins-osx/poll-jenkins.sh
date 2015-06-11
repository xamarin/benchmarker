#!/bin/bash

ARCH=amd64
LABEL="debian-$ARCH"

pushd `dirname "$0"` > /dev/null
BENCHMARKER_ROOT=`pwd`
BENCHMARKER_ROOT=`dirname "$BENCHMARKER_ROOT"`
popd > /dev/null

TMP_DIR="/tmp/benchmarker.$RANDOM$RANDOM"
mkdir "$TMP_DIR"
if [ $? -ne 0 ] ; then
    echo "Error: Cannot create temporary directory $TMP_DIR"
    exit 1
fi

ERROR=0

finish () {
    rm -rf "$TMP_DIR"
    if [ $? -ne 0 ] ; then
	echo "Error: Cannot delete temporary directory $TMP_DIR"
	ERROR=1
    fi

    exit $ERROR
}

JENKINS_JSON="$TMP_DIR/jenkins.json"
RUN_JSON="$TMP_DIR/run.json"

while true ; do

    while true ; do
	curl 'https://jenkins.mono-project.com/view/All/job/build-package-dpkg-mono/api/json?pretty=true&depth=10&tree=lastCompletedBuild%5Burl,runs%5Burl,artifacts%5B*%5D,fingerprint%5Boriginal%5B*%5D%5D%5D%5D' >"$JENKINS_JSON"
	if [ $? -ne 0 ] ; then
	    echo "Error: Cannot fetch JSON from Jenkins."
	    sleep 60
	    continue
	fi

	cat "$JENKINS_JSON" | jq -r ".lastCompletedBuild.runs | map(select(.url | contains(\"label=$LABEL\"))) | add" >"$RUN_JSON"
	if [ $? -ne 0 ] ; then
	    echo "Error: Cannot get run from JSON."
	    sleep 60
	    continue
	fi

	RUN_URL=`cat "$RUN_JSON" | jq -r '.url'`
	if [ $? -ne 0 ] ; then
	    echo "Error: Cannot get URL from JSON."
	    sleep 60
	    continue
	fi

	if [ "x$LAST_RUN_URL" != "x$RUN_URL" ] ; then
	    break
	fi

	echo "No new run - sleeping 5 minutes."
	sleep 300
    done

    GIT_FETCH_ID=`cat "$RUN_JSON" | jq -r '.fingerprint | map (select(.original.name == "build-source-tarball-mono")) | add | .original.number'`
    if [ $? -ne 0 ] ; then
	echo "Error: Cannot get URL from JSON."
	sleep 60
	continue
    fi

    ASM_PATH=`cat "$RUN_JSON" | jq -r '.artifacts | map (.relativePath) | map(select(contains(".changes") | not)) | map(select(contains("assemblies")))[0]'`
    if [ $? -ne 0 ] ; then
	echo "Error: Cannot get assemblies package from JSON."
	sleep 60
	continue
    fi
    ASM_URL="$RUN_URL/artifact/$ASM_PATH"

    BIN_PATH=`cat "$RUN_JSON" | jq -r ".artifacts | map (.relativePath) | map(select(contains(\".changes\") | not)) | map(select(contains(\"$ARCH\")))[0]"`
    if [ $? -ne 0 ] ; then
	echo "Error: Cannot get binary package from JSON."
	sleep 60
	continue
    fi
    BIN_URL="$RUN_URL/artifact/$BIN_PATH"

    COMMIT_SHA=`curl "https://jenkins.mono-project.com/job/build-source-tarball-mono/$GIT_FETCH_ID/pollingLog/pollingLog" | awk '/Latest remote/ { print $NF }'`
    if [ $? -ne 0 ] ; then
	echo "Error: Cannot get commit SHA."
	sleep 60
	continue
    fi

    echo $RUN_URL
    echo $ASM_URL
    echo $BIN_URL
    echo $GIT_FETCH_ID
    echo $COMMIT_SHA

    "$BENCHMARKER_ROOT/jenkins-osx/run.sh" --config auto-sgen --commit "$COMMIT_SHA" --build-url "$RUN_URL" --deb-urls "$ASM_URL" "$BIN_URL"
    if [ $? -ne 0 ] ; then
	echo "Error: Could not run benchmark."
	sleep 60
	continue
    fi

    LAST_RUN_URL="$RUN_URL"

done

finish
