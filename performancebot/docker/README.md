# Deployment

We deploy on an EC2 instance via Docker and Docker Compose.

## Docker requirements

At least `docker=1.7.1` is required:

    $ echo deb http://get.docker.com/ubuntu docker main | sudo tee /etc/apt/sources.list.d/docker.list
    $ sudo apt-key adv --keyserver pgp.mit.edu --recv-keys 36A1D7869245C8950F966E92D8576A8BA88D21E9
    $ sudo apt-get update
    $ sudo apt-get install -y lxc-docker-1.7.1
    $ sudo usermod -G docker -a `whoami`

And we need [docker-compose](https://github.com/docker/compose/releases).

## Produce the Docker Compose config file

On your machine:

    ./accreditize.sh

This produces `docker-compose.yml`.  Copy it to the deployment
machine.

## Log in to Amazon Docker registry

On your machine, run

    aws ecr get-login --region us-east-1

and run the command it outputs on the deployment machine.

The login will be valid for 10 hours.  Whenever you pull new images,
you'll have to do this again.

## EBS

The EBS volume has to be mounted to `/ebs`.

## Start the containers

On the deployment machine, in the directory where you put
`docker-compose.yml`:

    docker-compose up

# Redeployment

## Build and push the image to the registry

Each Docker image source directory (see below) has a `build.sh`
script, which builds the image and pushes it onto the Amazon Docker
registry, provided you're logged in (see above).

## Pull the images

On the deployment machine, in the directory with `docker-compose.yml`,
do

    docker-compose pull

## Restart the containers

Restart the containers you updated with

    docker-compose restart NAME

where `NAME` is the name of the container as given in
`docker-compose.yml`.

# List of all the images

## reloadcache

    https://github.com/schani/reloadcache

## http-api

    benchmarker/http-api

## pbot-master and pbot-slave

    benchmarker/performancebot/docker

## nginx

    benchmarker/performancebot/docker/nginx-ssl-reverse-proxy

# Cleanup docker images

Playing garbage collector.

    $ docker info # check device mapper status
    $ docker rm $(docker ps -a -q)  # delete stopped containers
    $ docker rmi $(docker images --filter "dangling=true" -q) # remove dangling layers
    $ docker info # verify
