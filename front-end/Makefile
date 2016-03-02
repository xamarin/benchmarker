#
# Stuff you should feel safe changing
#

PAGES = compare config machine timeline runset pullrequest pullrequests pausetimes
COMMON = common.tsx utils.ts charts.tsx database.ts outliers.ts

#
# Sources and targets
#

COMMON_SOURCES = $(addprefix src/,$(COMMON))
PAGE_SOURCES = $(addprefix src/,$(addsuffix .tsx,$(PAGES)))
PAGE_TARGETS = $(addprefix build/,$(addsuffix .js,$(PAGES)))

DEP_TARGETS = $(addprefix node_modules/,$(shell echo $(DEPS) | sed -E 's/@[^ ]+//g'))

#
# Entry points
#
# "all" is the default target and should remain first. "clean" assumes that all
# build artifacts are placed in the "build" directory.
#

.PHONY : all
all : $(PAGE_TARGETS)

.PHONY : lint
lint :
	@echo 'Linting...'
	@npm install
	@./node_modules/tslint/bin/tslint src/*.ts src/*.tsx

.PHONY : clean
clean :
	@echo 'Cleaning...'
	@rm -rf build

.PHONY : server
server :
	@echo 'Connect to http://localhost:8080/webpack-dev-server/index.html'
	@npm install
	@npm start

#
# Pages
#
# We use a dummy file as a witness of the webpack build, partly to avoid
# redundantly specifying the information in webpack.config.js, and partly to
# ensure that concurrent builds don't lead to redundant/racy rebuilds.
#

$(PAGE_TARGETS) : build/witness

build/witness : $(PAGE_SOURCES) $(COMMON_SOURCES) Makefile webpack.config.js build
	@echo 'Packing webs...'
	@npm install
	@node ./node_modules/webpack/bin/webpack.js --optimize-minimize --bail
	@touch build/witness

build :
	@mkdir -p build
