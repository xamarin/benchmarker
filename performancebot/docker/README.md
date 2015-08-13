# Docker requirements

at least `docker=1.7.1` is required:

    $ echo deb http://get.docker.com/ubuntu docker main | sudo tee /etc/apt/sources.list.d/docker.list
    $ sudo apt-key adv --keyserver pgp.mit.edu --recv-keys 36A1D7869245C8950F966E92D8576A8BA88D21E9
    $ sudo apt-get update
    $ sudo apt-get install -y lxc-docker-1.7.1
    $ sudo usermod -G docker -a `whoami`

# Master setup

* make sure an EBS volume is mounted to `/ebs`
* make sure `/ebs` is owned by your user

`docker` should put its images on said EBS volume:

    $ mkdir /ebs/docker
    $ echo 'DOCKER_OPTS="-g /ebs/docker"' | sudo tee -a $EDITOR /etc/defaults/docker
    $ sudo service docker restart


`scp Dockerfile.master` from another machine:

    $ docker build -f Dockerfile.master -t pbot-master .
    $ docker run -p 80:8010 -p 9989:9989 -p 443:9999 -v /ebs:/ebs -it pbot-master

wait after initialization is done, if you see the shell prompt press `CTRL+P
CTRL+Q` in order to detach from the container.
You are required to deploy the master config from your machine, as it requires
private files. Run this from the `benchmarker/performancebot` directory on your
machine:

    $ export EC2PBOTMASTERIP=ip.of.the.master.com
    $ make ec2-deploy

# Slave setup
`scp Dockerfile.ec2slave` from another machine. Note that you need to configure
the master configuration accordingly with `$WANTED_SLAVE_NAME` and
`$SECRET_SLAVE_PASSWORD`:

    $ docker build -f Dockerfile.ec2slave -t pbot-slave .
    $ docker run -h $WANTED_SLAVE_NAME -it pbot-slave ip.of.the.master.com $SECRET_SLAVE_PASSWORD

after machine setup you are supposed to see output by the `buildslave`, press
`CTRL+P  CTRL+Q` in order to detach from the container.
