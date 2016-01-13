#
# Stuff you should feel safe changing
#

PAGES = compare config machine timeline runset pullrequest pullrequests pausetimes
COMMON = common.tsx utils.ts charts.tsx database.ts outliers.ts
DEPS = react@0.14.2 github-api@0.10.6 less@2.5.3 react-dom@0.14.2 ts-loader@0.6.0 typescript@1.6.2 webpack@1.12.2
STYLES = \
	colors \
	common \
	compare \
	font-awesome.min \
	navigation \
	pausetimes \
	pullrequest \
	pullrequests \
	runset \
	timeline

#
# Sources and targets
#

COMMON_SOURCES = $(addprefix src/,$(COMMON))
PAGE_SOURCES = $(addprefix src/,$(addsuffix .tsx,$(PAGES)))
PAGE_TARGETS = $(addprefix build/,$(addsuffix .js,$(PAGES)))
STYLE_TARGETS = $(addprefix build/,$(addsuffix .css,$(STYLES)))
STYLE_DEPS = $(addprefix build/,$(addsuffix .css.dep,$(STYLES)))

DEP_TARGETS = $(addprefix node_modules/,$(shell echo $(DEPS) | sed -E 's/@[^ ]+//g'))

#
# Entry points
#
# "all" is the default target and should remain first. "clean" assumes that all
# build artifacts are placed in the "build" directory.
#

.PHONY : all
all : $(PAGE_TARGETS) $(STYLE_TARGETS)

.PHONY : lint
lint :
	@echo 'Linting...'
	@tslint src/*.ts src/*.tsx

.PHONY : clean
clean :
	@echo 'Cleaning...'
	@rm -rf build

.PHONY :
server : $(DEP_TARGETS)
	@echo 'Connect to http://localhost:8080/webpack-dev-server/index.html'
	@DEV_SERVER=true webpack-dev-server --devtool=inline-source-map

#
# Pages
#
# We use a dummy file as a witness of the webpack build, partly to avoid
# redundantly specifying the information in webpack.config.js, and partly to
# ensure that concurrent builds don't lead to redundant/racy rebuilds.
#

$(PAGE_TARGETS) : build/witness

build/witness : $(PAGE_SOURCES) $(COMMON_SOURCES) $(DEP_TARGETS) Makefile webpack.config.js build
	@echo 'Packing webs...'
	@webpack --optimize-minimize --bail
	@touch build/witness

#
# Dependencies
#

define DEP_RULE
node_modules/$(shell echo $1 | sed -E 's/@[^ ]+//g') :
	@echo 'Installing $1...'
	@npm install "$1"

endef

$(foreach DEP,$(DEPS),$(eval $(call DEP_RULE,$(DEP))))

build :
	@mkdir -p build

#
# Styles
#
# We use macros (STYLE_RULE) instead of wildcards (%.css : ...) so that only
# those styles explicitly named in STYLES are included.
#

define STYLE_DEP_RULE
build/$1.css.dep : src/$1.less Makefile build
	@lessc --depends src/$1.less build/$1.css > build/$1.css.dep

endef

$(foreach STYLE,$(STYLES),$(eval $(call STYLE_DEP_RULE,$(STYLE))))

define STYLE_RULE
build/$1.css : src/$1.less build/$1.css.dep Makefile build
	@echo "Building style '$1'..."
	@lessc src/$1.less build/$1.css

endef

$(foreach STYLE,$(STYLES),$(eval $(call STYLE_RULE,$(STYLE))))

-include $(STYLE_DEPS)
