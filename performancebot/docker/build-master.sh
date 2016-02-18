#!/bin/bash

set -e

docker build -f Dockerfile.master -t pbot-master .

docker tag -f pbot-master 633007691302.dkr.ecr.us-east-1.amazonaws.com/pbot-master:latest
docker push 633007691302.dkr.ecr.us-east-1.amazonaws.com/pbot-master:latest
