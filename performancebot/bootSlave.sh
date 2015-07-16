#!/bin/bash

set -e

: ${EC2PBOTMASTERIP?"need to set master ip in EC2PBOTMASTERIP"}
: ${BUILDBOTSLAVEPWD?"need to set slave password (configured in master instance) in BUILDBOTSLAVEPWD"}

for i in "/usr/bin/dpkg" "/bin/cp" "/bin/rm"; do
    sudo -n "$i" --help &> /dev/null || (echo "/etc/sudoers must have the following line:" && echo "`whoami` `hostname` = (root) NOPASSWD: $i" && exit 1)
done

echo "TODO: http://www.mono-project.com/docs/getting-started/install/linux/#debian-ubuntu-and-derivatives"

sudo apt-get install python-pip python-dev mono-runtime
sudo pip install virtualenv

# cleanup previous instance
sudo /bin/rm -rf $HOME/slave

mkdir -p $HOME/slave/env
virtualenv $HOME/slave/env
source $HOME/slave/env/bin/activate

cd $HOME/slave
pip install buildbot-slave

buildslave create-slave slavedir $EC2PBOTMASTERIP `hostname` $BUILDBOTSLAVEPWD
buildslave start slavedir
