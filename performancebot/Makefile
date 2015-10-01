SHELL := bash

ifndef EC2PBOTMASTERIP
$(error EC2PBOTMASTERIP is not set)
endif

# DEBUGSHELL:=-x

PBOTMASTERROOT:=/ebs/pbot-master/performancebot
REMOTEHOST=ubuntu@$(EC2PBOTMASTERIP)
PYTHONFILES:=master.cfg jenkinspoller.py monofactory.py monosteps.py slack.py googleoauth2.py constants.py checkretry.py wrenchpoller.py

# check that there is only a given number of docker containers are running, that ensures we're interacting with the correct one.
CONTAINERCHECK=check() { test `ssh $(REMOTEHOST) "docker ps | grep pbot-master | awk '{print $$1}' | wc -l"` -eq $$1 || (echo "more than $$1 container is running on the ec2 instance"; exit 1) }; check

.PHONY: ec2-boot ec2-kill ec2-start ec2-stop ec2-follow-log ec2-deploy pylint

# start container on ec2 instance
ec2-boot:
	@$(CONTAINERCHECK) 0
	ssh $(REMOTEHOST) 'docker run --name pbotmaster -p 9989:9989 -p 9999:9999 -v /ebs:/ebs -idt pbot-master'

# stop container on ec2 instance
ec2-kill:
	@$(CONTAINERCHECK) 1
	ssh $(REMOTEHOST) 'docker stop `docker ps | grep pbot-master | awk '"'"'{print $$1}'"'"'`'
	@$(CONTAINERCHECK) 0

# start buildbot master instance (requires running ec2 instance)
ec2-start:
	@$(CONTAINERCHECK) 1
	ssh $(REMOTEHOST) 'docker exec `docker ps | grep pbot-master | awk '"'"'{print $$1}'"'"'` buildbot start $(PBOTMASTERROOT)'

# stop buildbot master instance (requires running ec2 instance)
ec2-stop:
	@$(CONTAINERCHECK) 1
	ssh $(REMOTEHOST) 'docker exec `docker ps | grep pbot-master | awk '"'"'{print $$1}'"'"'` buildbot stop $(PBOTMASTERROOT)'

# follow buildbot master log messages
ec2-follow-log:
	@$(CONTAINERCHECK) 1
	ssh $(REMOTEHOST) 'tail -f $(PBOTMASTERROOT)/twistd.log'

# deploy buildbot configuration from this working directory and reconfig buildbot instance; if reconfig fails, the old configuration is restored
ec2-deploy: $(PYTHONFILES) credentials.py forcec0state.sh benchmarkerCredentials public_html templates
	@$(CONTAINERCHECK) 1
	@ssh $(REMOTEHOST) 'mkdir -p /ebs/tmp'
	# copy files.
	scp -q -r $^ $(REMOTEHOST):/ebs/tmp/
	# create backup of current buildbot configuration and copy new files.
	ssh $(REMOTEHOST) 'docker exec `docker ps | grep pbot-master | awk '"'"'{print $$1}'"'"'` bash $(DEBUGSHELL) -c "for i in $^; do if [ -d /ebs/tmp/\$$i ]; then rm -rf $(PBOTMASTERROOT)/\$$i~ && mv $(PBOTMASTERROOT)/\$$i{,~}; fi; cp -b -r /ebs/tmp/\$$i $(PBOTMASTERROOT)/; done"'
	# reconfig buildbot master and depending on the result revert to backup or delete it.
	ssh $(REMOTEHOST) 'docker exec `docker ps | grep pbot-master | awk '"'"'{print $$1}'"'"'` bash $(DEBUGSHELL) -c "test -f $(PBOTMASTERROOT)/twistd.pid && kill -0 \$$(cat $(PBOTMASTERROOT)/twistd.pid) ; (if [ \$$? -ne 0 ] ; then buildbot start $(PBOTMASTERROOT); else buildbot reconfig $(PBOTMASTERROOT); fi || (echo \"deployment failed, restoring files\" && (for i in $^ ; do rm -r $(PBOTMASTERROOT)/\$$i && mv $(PBOTMASTERROOT)/\$$i{~,} ; done))) && rm -rf $(PBOTMASTERROOT)/*~"'

pylint:
	pylint --rcfile=.pylintrc $(PYTHONFILES)
