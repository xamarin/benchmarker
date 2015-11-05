# Stuff you should feel safe changing

PAGES = compare config machine timeline runset pullrequest pullrequests pausetimes
COMMON = common.tsx utils.ts charts.tsx database.ts outliers.ts
DEPS = react github-api react-dom ts-loader typescript webpack
STYLES = common compare style timeline pullrequest pullrequests runset font-awesome.min

# Sources and targets

COMMON_SOURCES = $(addprefix src/,$(COMMON))
PAGE_SOURCES = $(addprefix src/,$(addsuffix .tsx,$(PAGES)))
PAGE_TARGETS = $(addprefix build/,$(addsuffix .js,$(PAGES)))
STYLE_TARGETS = $(addprefix build/,$(addsuffix .css,$(STYLES)))
DEP_TARGETS = $(addprefix node_modules/,$(DEPS))

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

.PHONY :
flow :
	( mv node_modules node_modules_save ; flow check ; ERR=$$? ; mv node_modules_save node_modules ; exit $$ERR )

# Pages

$(PAGE_TARGETS) : build/witness

build/witness : $(PAGE_SOURCES) $(COMMON_SOURCES) $(DEP_TARGETS) Makefile webpack.config.js build
	@echo 'Packing webs...'
	@webpack --optimize-minimize --bail
	@touch build/witness

# Dependencies

define DEP_RULE
node_modules/$1 :
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
