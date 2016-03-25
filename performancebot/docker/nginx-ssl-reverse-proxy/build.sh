#!/bin/bash

set -e

if [ ! -f server.crt -o ! -f server.key ] ; then
    ./setup_certs.sh
fi

docker build -t nginx .

docker tag nginx 633007691302.dkr.ecr.us-east-1.amazonaws.com/nginx:latest
docker push 633007691302.dkr.ecr.us-east-1.amazonaws.com/nginx:latest
