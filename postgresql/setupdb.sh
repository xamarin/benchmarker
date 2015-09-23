#!/bin/bash

set -o pipefail

if [ "$#" -ne 1 ] ; then
    echo "Usage: setupdb.sh PARSE-EXPORT-DIR"
    exit 1
fi

PARSE_EXPORT_DIR="$1"

if [ ! -d "$PARSE_EXPORT_DIR" ] ; then
    echo "Error: The Parse export directory $PARSE_EXPORT_DIR does not exist."
    exit 1
fi

ADMIN_JSON=`mono ../tools/Accreditize/bin/Debug/Accreditize.exe adminPostgres`
if [ $? -ne 0 ] ; then
    echo "Error: Accreditize failed."
    exit 1
fi

BENCHMARKER_JSON=`mono ../tools/Accreditize/bin/Debug/Accreditize.exe benchmarkerPostgres`
if [ $? -ne 0 ] ; then
    echo "Error: Accreditize failed."
    exit 1
fi

POSTGREST_JSON=`mono ../tools/Accreditize/bin/Debug/Accreditize.exe postgrestPostgres`
if [ $? -ne 0 ] ; then
    echo "Error: Accreditize failed."
    exit 1
fi

export PGUSER=`echo "$ADMIN_JSON" | jq -r '.user'`
if [ $? -ne 0 ] ; then
    echo "Error: jq failed."
    exit 1
fi

ADMIN_DATABASE=`echo "$ADMIN_JSON" | jq -r '.database'`
if [ $? -ne 0 ] ; then
    echo "Error: jq failed."
    exit 1
fi

export PGHOST=`echo "$BENCHMARKER_JSON" | jq -r '.host'`
if [ $? -ne 0 ] ; then
    echo "Error: jq failed."
    exit 1
fi

export PGPORT=`echo "$BENCHMARKER_JSON" | jq -r '.port'`
if [ $? -ne 0 ] ; then
    echo "Error: jq failed."
    exit 1
fi

DATABASE=`echo "$BENCHMARKER_JSON" | jq -r '.database'`
if [ $? -ne 0 ] ; then
    echo "Error: jq failed."
    exit 1
fi

BENCHMARKER_USER=`echo "$BENCHMARKER_JSON" | jq -r '.user'`
if [ $? -ne 0 ] ; then
    echo "Error: jq failed."
    exit 1
fi

BENCHMARKER_PASSWORD=`echo "$BENCHMARKER_JSON" | jq -r '.password'`
if [ $? -ne 0 ] ; then
    echo "Error: jq failed."
    exit 1
fi

POSTGREST_USER=`echo "$POSTGREST_JSON" | jq -r '.user'`
if [ $? -ne 0 ] ; then
    echo "Error: jq failed."
    exit 1
fi

POSTGREST_PASSWORD=`echo "$POSTGREST_JSON" | jq -r '.password'`
if [ $? -ne 0 ] ; then
    echo "Error: jq failed."
    exit 1
fi

cat dropowned.psql.in | sed "s/\$USER/$POSTGREST_USER/g" | PGPASSWORD="$POSTGREST_PASSWORD" psql -d "$DATABASE" -U "$POSTGREST_USER" -f -
cat dropowned.psql.in | sed "s/\$USER/$BENCHMARKER_USER/g" | PGPASSWORD="$BENCHMARKER_PASSWORD" psql -d "$DATABASE" -U "$BENCHMARKER_USER" -f -

export PGPASSWORD=`echo "$ADMIN_JSON" | jq -r '.password'`
if [ $? -ne 0 ] ; then
    echo "Error: jq failed."
    exit 1
fi

cat drop.psql.in | sed "s/\$BENCHMARKER_USER/$BENCHMARKER_USER/g" | sed "s/\$POSTGREST_USER/$POSTGREST_USER/g" | psql -d "$DATABASE" -f -
if [ $? -ne 0 ] ; then
    echo "Error: psql failed."
    exit 1
fi

cat init.psql.in | sed "s/\$BENCHMARKER_USER/$BENCHMARKER_USER/g" | sed "s/\$BENCHMARKER_PASSWORD/$BENCHMARKER_PASSWORD/g" | sed "s/\$DATABASE/$DATABASE/g" | psql -d "$ADMIN_DATABASE" -f -
if [ $? -ne 0 ] ; then
    echo "Error: psql failed."
    exit 1
fi

mono ../tools/Parse2Postgres/bin/Debug/Parse2Postgres.exe "$PARSE_EXPORT_DIR"
if [ $? -ne 0 ] ; then
    echo "Error: Parse2Postgres failed."
    exit 1
fi

cat restructure.psql.in | sed "s/\$BENCHMARKER_USER/$BENCHMARKER_USER/g" | psql -d "$DATABASE" -f -
if [ $? -ne 0 ] ; then
    echo "Error: psql failed."
    exit 1
fi

cat views.psql.in | sed "s/\$POSTGREST_USER/$POSTGREST_USER/g" | sed "s/\$POSTGREST_PASSWORD/$POSTGREST_PASSWORD/g" | psql -d "$DATABASE" -f -
if [ $? -ne 0 ] ; then
    echo "Error: psql failed."
    exit 1
fi
