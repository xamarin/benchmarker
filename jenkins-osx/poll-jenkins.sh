#!/bin/bash

JQ_JOIN="reduce .[] as \$item (\"\"; . + \"\n\" + \$item)"

ARCH=amd64
LABEL="debian-$ARCH"
HOSTNAME=`uname -n`

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
BUILDS_TESTED_LIST="$TMP_DIR/builds-tested"
BUILDS_ALL_LIST="$TMP_DIR/builds-all"

while true ; do

    while true ; do
	# get all the builds we've tested from Parse
	curl -X GET \
	     -H "X-Parse-Application-Id: 7khPUBga9c7L1YryD1se1bp6VRzKKJESc0baS9ES" \
	     -H "X-Parse-REST-API-Key: xOHOwaDls0fcuMKLIH0nzaMKclLzCWllwClLej4d" \
	     -G \
	     --data-urlencode "where={\"buildURL\":{\"\$exists\":true},\"machine\":{\"\$inQuery\":{\"where\":{\"name\":\"$HOSTNAME\"},\"className\":\"Machine\"}}}" \
	     https://api.parse.com/1/classes/RunSet | jq -r ".results | map(.buildURL) | sort | unique | $JQ_JOIN" >"$BUILDS_TESTED_LIST"
	if [ $? -ne 0 ] ; then
	    echo "Error: Cannot fetch JSON from Parse."
	    sleep 60
	    continue
	fi

	# get information on all builds Jenkins has built
	curl "https://jenkins.mono-project.com/view/All/job/build-package-dpkg-mono/label=$LABEL/api/json?pretty=true&tree=allBuilds\[fingerprint\[original\[*\]\],artifacts\[*\],url,building\]" | jq ".allBuilds | map(select(.building | not))" >"$JENKINS_JSON"
	if [ $? -ne 0 ] ; then
	    echo "Error: Cannot fetch JSON from Jenkins."
	    sleep 60
	    continue
	fi

	# filter out the URLs from the list from Jenkins
	cat "$JENKINS_JSON" | jq -r "map(.url) | sort | unique | $JQ_JOIN" >"$BUILDS_ALL_LIST"
	if [ $? -ne 0 ] ; then
	    echo "Error: Cannot get all builds from JSON."
	    sleep 60
	    continue
	fi

	# get a list of all the runs we haven't yet tested
	RUN_URL=`comm -2 -3 "$BUILDS_ALL_LIST" "$BUILDS_TESTED_LIST" | tail -n 1`
	if [ "x$RUN_URL" = "x" ] ; then
	    echo "We've tested all the builds.  Sleeping."
	    sleep 300
	    continue
	fi

	echo "Build to test is $RUN_URL"

	cat "$JENKINS_JSON" | jq -r "map(select(.url==\"$RUN_URL\")) | add" >"$RUN_JSON"
	if [ $? -ne 0 ] ; then
	    echo "Error: Cannot get run from JSON."
	    sleep 60
	    continue
	fi

	break
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
    if [ $? -ne 0 -o "x$COMMIT_SHA" = "x" ] ; then
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

done

finish
