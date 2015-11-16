# Stuff you should feel safe changing

PAGES = compare config machine timeline runset pullrequest pullrequests pausetimes
COMMON = common.tsx utils.ts charts.tsx database.ts outliers.ts
DEPS = react@0.14.2 github-api@0.10.6 react-dom@0.14.2 ts-loader@0.6.0 typescript@1.6.2 webpack@1.12.2
STYLES = common compare style timeline pullrequest pullrequests runset font-awesome.min

# Sources and targets

COMMON_SOURCES = $(addprefix src/,$(COMMON))
PAGE_SOURCES = $(addprefix src/,$(addsuffix .tsx,$(PAGES)))
PAGE_TARGETS = $(addprefix build/,$(addsuffix .js,$(PAGES)))
STYLE_TARGETS = $(addprefix build/,$(addsuffix .css,$(STYLES)))

DEP_TARGETS = $(addprefix node_modules/,$(shell echo $(DEPS) | sed -E 's/@[^ ]+//g'))

# Entry points

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
server :
	@echo 'Connect to http://localhost:8080/webpack-dev-server/index.html'
	@DEV_SERVER=true webpack-dev-server --devtool=inline-source-map

# Pages

$(PAGE_TARGETS) : build/witness

build/witness : $(PAGE_SOURCES) $(COMMON_SOURCES) $(DEP_TARGETS) Makefile webpack.config.js build
	@echo 'Packing webs...'
	@webpack --optimize-minimize --bail
	@touch build/witness

# Dependencies

define DEP_RULE
node_modules/$(shell echo $1 | sed -E 's/@[^ ]+//g') :
	@echo 'Installing $1...'
	@npm install "$1"

endef

$(foreach DEP,$(DEPS),$(eval $(call DEP_RULE,$(DEP))))

build :
	@mkdir -p build

# Styles

define STYLE_RULE
build/$1.css : src/$1.css Makefile build
	@echo "Building style '$1'..."
	@cp -f src/$1.css build/$1.css

endef

$(foreach STYLE,$(STYLES),$(eval $(call STYLE_RULE,$(STYLE))))
