#!/bin/bash

set -e

if [ $# -ne 2 -a $# -ne 3 ]; then
    echo "./bootSlave.sh <masterhost> <slavepwd> [buildbotslavename]"
    exit 1
fi

EC2PBOTMASTERIP="$1"
BUILDBOTSLAVEPWD="$2"

if [ $# -eq 3 ]; then
    SLAVEHOSTNAME="$3"
else
    SLAVEHOSTNAME=`hostname`
fi

for i in "/usr/bin/dpkg" "/bin/cp" "/bin/rm"; do
    sudo -k -n "$i" --help &> /dev/null || (echo "/etc/sudoers must have the following line:" && echo "`whoami` `hostname` = (root) NOPASSWD: $i" && exit 1)
done

sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
echo "deb http://download.mono-project.com/repo/debian wheezy main" | sudo tee /etc/apt/sources.list.d/mono-xamarin.list
echo "deb http://download.mono-project.com/repo/debian wheezy-apache24-compat main" | sudo tee -a /etc/apt/sources.list.d/mono-xamarin.list
sudo apt-get update

sudo apt-get install -y python-pip python-dev mono-runtime mono-devel git-core wget
sudo pip install virtualenv

# cleanup previous instance
sudo /bin/rm -rf $HOME/slave

mkdir -p $HOME/slave/env
virtualenv $HOME/slave/env
source $HOME/slave/env/bin/activate

cd $HOME/slave
pip install buildbot-slave==0.8.12

buildslave create-slave --keepalive 45 slavedir $EC2PBOTMASTERIP $SLAVEHOSTNAME $BUILDBOTSLAVEPWD
buildslave start --nodaemon slavedir
