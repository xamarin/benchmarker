#!/bin/bash

set -e

`aws ecr get-login --region us-east-1`
BASE64=`base64 ~/.docker/config.json`
cat secret.json.in | sed "s/\$BASE64/$BASE64/g" >/tmp/secret.json
kubectl replace -f /tmp/secret.json
