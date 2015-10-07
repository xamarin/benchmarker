.PHONY : all
all :

.PHONY : deploy

deploy :
	@parse version &> /dev/null || (echo 'need to install parse command line tool, via https://parse.com/apps/quickstart#cloud_code/unix' && exit 2)
	parse deploy

.PHONY : lint
lint :
	@echo 'Linting...'
	@eslint -f compact cloud
