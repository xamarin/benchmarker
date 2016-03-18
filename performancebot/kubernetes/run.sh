#!/bin/bash

K8S_VERSION=1.2.0
#REGISTRY=gcr.io/google_containers
REGISTRY=633007691302.dkr.ecr.us-east-1.amazonaws.com

docker run \
    --volume=/:/rootfs:ro \
    --volume=/sys:/sys:ro \
    --volume=/var/lib/docker/:/var/lib/docker:rw \
    --volume=/var/lib/kubelet/:/var/lib/kubelet:rw \
    --volume=/var/run:/var/run:rw \
    --net=host \
    --pid=host \
    --privileged=true \
    -d \
    ${REGISTRY}/hyperkube-amd64:v${K8S_VERSION} \
    /hyperkube kubelet \
        --containerized \
        --hostname-override="127.0.0.1" \
        --address="0.0.0.0" \
        --api-servers=http://localhost:8080 \
        --config=/etc/kubernetes/manifests \
        --allow-privileged=true --v=2

#        --cluster-dns=10.0.0.10 \
#        --cluster-domain=cluster.local \

#ssh docker@$(docker-machine ip default) -L 8080:localhost:8080
