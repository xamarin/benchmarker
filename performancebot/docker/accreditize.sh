#!/bin/bash

set -o pipefail

POSTGREST_JSON=`mono ../../tools/Accreditize/bin/Debug/Accreditize.exe postgrestPostgres`
if [ $? -ne 0 ] ; then
    echo "Error: Accreditize failed."
    exit 1
fi

export DBHOST=`echo "$POSTGREST_JSON" | jq -r '.host'`
if [ $? -ne 0 ] ; then
    echo "Error: jq failed."
    exit 1
fi

export DBPORT=`echo "$POSTGREST_JSON" | jq -r '.port'`
if [ $? -ne 0 ] ; then
    echo "Error: jq failed."
    exit 1
fi

export DBNAME=`echo "$POSTGREST_JSON" | jq -r '.database'`
if [ $? -ne 0 ] ; then
    echo "Error: jq failed."
    exit 1
fi

export DBUSER=`echo "$POSTGREST_JSON" | jq -r '.user'`
if [ $? -ne 0 ] ; then
    echo "Error: jq failed."
    exit 1
fi

export DBPASS=`echo "$POSTGREST_JSON" | jq -r '.password'`
if [ $? -ne 0 ] ; then
    echo "Error: jq failed."
    exit 1
fi

PBOT_JSON=`mono ../../tools/Accreditize/bin/Debug/Accreditize.exe pbotSlaves`
if [ $? -ne 0 ] ; then
    echo "Error: Accreditize failed."
    exit 1
fi

export EC2_SLAVE1=`echo "$PBOT_JSON" | jq -r '.["ec2-slave1"]'`
if [ $? -ne 0 ] ; then
    echo "Error: jq failed."
    exit 1
fi

export EC2_SLAVE2=`echo "$PBOT_JSON" | jq -r '.["ec2-slave2"]'`
if [ $? -ne 0 ] ; then
    echo "Error: jq failed."
    exit 1
fi

cat postgrest.yml-TEMPLATE | sed "s/\$DBHOST/$DBHOST/g" | sed "s/\$DBPORT/$DBPORT/g" | sed "s/\$DBNAME/$DBNAME/g" | sed "s/\$DBUSER/$DBUSER/g" | sed "s/\$DBPASS/$DBPASS/g" >postgrest.yml
cat behind-nginx.yml-TEMPLATE | sed "s/\$EC2_SLAVE1/$EC2_SLAVE1/g" | sed "s/\$EC2_SLAVE2/$EC2_SLAVE2/g" >behind-nginx.yml
