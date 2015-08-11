# Setup Instructions for [Utilite2](http://www.compulab.co.il/utilite-computer/web/utilite2-overview)

Utilite2 is shipped with a pre-installed Ubuntu:

    $ cat /etc/issue
    Ubuntu-14.04-Linaro-14.10-Utilite2-1.1

Default User and Password: `utilite`/`111111`

It's highly recommended to connect this device to ethernet instead of using WiFi, because:

* WiFi connection is less reliable. We observed several disconnects and making the device unreachable up to 30 minutes.
* It looks like Utilite2 devices get assigned the *same* MAC address on the WiFi device.


Connect Utilite2 to a monitor via HDMI, turn it on and login with the credentials from above. Setup your own user:

    sudo useradd $NEWUSER -m
    sudo passwd $NEWUSER
    sudo usermod -a -G adm,sudo $NEWUSER
    sudo chsh -s /bin/bash $NEWUSER

logout and copy boot script for slaves to device:

    scp $BENCHMARKER_REPO/performancebot/bootSlave.sh $NEWUSER@$UTILITE2IP:~

Then login with `$NEWUSER` and run `bootSlave.sh`. The script will tell you that there're entries missing in `/etc/sudoers`, modify it accordingly.

    bash bootSlave.sh performancebot.mono-project.com $SECRET_SLAVE_PASSWORD

`bootSlave.sh` installs required packages via apt-get, and creates a `virtualenv` for the python libraries required for `buildbot`. After that, it will start the `buildbot` slave.
