#!/bin/bash

set -e

docker build -f Dockerfile.master -t pbot-slave .

docker tag -f pbot-slave 633007691302.dkr.ecr.us-east-1.amazonaws.com/pbot-slave:latest
docker push 633007691302.dkr.ecr.us-east-1.amazonaws.com/pbot-slave:latest
