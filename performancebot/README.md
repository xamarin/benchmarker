# Prerequisites

See `../.travis.yml` on which packages to `pip install`.

# pylint

    env EC2PBOTMASTERIP=XXX make pylint

# Deployment

    env EC2PBOTMASTERIP=performancebot.mono-project.com make ec2-deploy

# Running a slave

Assuming it's been added and the hostname is correct:

    ./bootSlave.sh performancebot.mono-project.com SLAVE-PASSWORD

# Resetting the last built revision for a builder

On `ec2pbot` in `/ebs/pbot-master/performancebot` there's a SQLite database `state.sqlite`:

    ubuntu@ip-172-30-3-139:/ebs/pbot-master/performancebot$ sudo sqlite3 state.sqlite

    sqlite> select * from object_state os, objects o where os.name = 'current_rev' and os.objectid = o.id;
	....
    98|current_rev|"1874"|98|debian-amd64#ec2-slave1#auto-sgen-cachegrind|MonoJenkinsPoller-0-debian-amd64-ec2-slave1-auto-sgen-cachegrindvalgrind-cachegrind_
	....
    sqlite> update object_state set value_json='"1"' where objectid=98;
