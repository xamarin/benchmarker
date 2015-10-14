# Stuff you should feel safe changing

PAGES = compare config machine timeline runset pullrequest
COMMON = common utils charts database outliers
DEPS = babel-loader react github-api react-dom
STYLES = common compare style timeline pullrequest runset font-awesome.min

# Sources and targets

COMMON_SOURCES = $(addprefix src/,$(addsuffix .js,$(COMMON)))
PAGE_SOURCES = $(addprefix src/,$(addsuffix .js,$(PAGES)))
PAGE_TARGETS = $(addprefix build/,$(addsuffix .js,$(PAGES)))
STYLE_TARGETS = $(addprefix build/,$(addsuffix .css,$(STYLES)))
DEP_TARGETS = $(addprefix node_modules/,$(DEPS))

# Entry points

.PHONY : all
all : $(PAGE_TARGETS) $(STYLE_TARGETS)

.PHONY : lint
lint :
	@echo 'Linting...'
	@eslint -f compact src

.PHONY : clean
clean :
	@echo 'Cleaning...'
	@rm -rf build

.PHONY :
server :
	@echo 'Connect to http://localhost:8080/webpack-dev-server/index.html'
	@webpack-dev-server --devtool=inline-source-map

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
