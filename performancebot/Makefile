SHELL := bash

ifndef EC2PBOTMASTERIP
$(error EC2PBOTMASTERIP is not set)
endif

DEBUGSHELL:=-x

PBOTMASTERROOT:=/ebs/pbot-master/performancebot
REMOTEHOST=ubuntu@$(EC2PBOTMASTERIP)

# check that there is only a given number of docker containers are running, that ensures we're interacting with the correct one.
CONTAINERCHECK=check() { test `ssh $(REMOTEHOST) "docker ps -q | wc -l"` -eq $$1 || (echo "more than $$1 container is running on the ec2 instance"; exit 1) }; check

.PHONY: ec2-boot ec2-kill ec2-start ec2-stop ec2-follow-log ec2-deploy

# start container on ec2 instance
ec2-boot:
	@$(CONTAINERCHECK) 0
	ssh $(REMOTEHOST) 'docker run -p 80:8010 -p 9989:9989 -v /ebs:/ebs -idt pbot-master'

# stop container on ec2 instance
ec2-kill:
	@$(CONTAINERCHECK) 1
	ssh $(REMOTEHOST) 'docker stop `docker ps -q`'
	@$(CONTAINERCHECK) 0

# start buildbot master instance (requires running ec2 instance)
ec2-start:
	@$(CONTAINERCHECK) 1
	ssh $(REMOTEHOST) 'docker exec `docker ps -q` buildbot start $(PBOTMASTERROOT)'

# stop buildbot master instance (requires running ec2 instance)
ec2-stop:
	@$(CONTAINERCHECK) 1
	ssh $(REMOTEHOST) 'docker exec `docker ps -q` buildbot stop $(PBOTMASTERROOT)'

# follow buildbot master log messages
ec2-follow-log:
	@$(CONTAINERCHECK) 1
	ssh $(REMOTEHOST) 'tail -f $(PBOTMASTERROOT)/twistd.log'

# deploy buildbot configuration from this working directory and reconfig buildbot instance; if reconfig fails, the old configuration is restored
ec2-deploy: master.cfg credentials.py jenkinspoller.py monofactory.py monosteps.py slack.py forcec0state.sh parse.pw public_html templates
	@$(CONTAINERCHECK) 1
	ssh $(REMOTEHOST) 'mkdir -p /ebs/tmp'
	scp -r $^ $(REMOTEHOST):/ebs/tmp/
	ssh $(REMOTEHOST) 'docker exec `docker ps -q` bash $(DEBUGSHELL) -c "for i in $^; do if [ -d /ebs/tmp/\$$i ]; then rm -rf $(PBOTMASTERROOT)/\$$i~ && mv $(PBOTMASTERROOT)/\$$i{,~}; fi; cp -b -r /ebs/tmp/\$$i $(PBOTMASTERROOT)/; done"'
	ssh $(REMOTEHOST) 'docker exec `docker ps -q` bash $(DEBUGSHELL) -c "test -f $(PBOTMASTERROOT)/twistd.pid && kill -0 \$$(cat $(PBOTMASTERROOT)/twistd.pid) ; (if [ \$$? -ne 0 ] ; then buildbot start $(PBOTMASTERROOT); else buildbot reconfig $(PBOTMASTERROOT); fi || (echo \"deployment failed, restoring files\" && (for i in $^ ; do rm -r $(PBOTMASTERROOT)/\$$i && mv $(PBOTMASTERROOT)/\$$i{~,} ; done))) && rm -rf $(PBOTMASTERROOT)/*~"'

