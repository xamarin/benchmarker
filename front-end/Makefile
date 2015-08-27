# Stuff you should feel safe changing

PAGES = compare config machine timeline runset pullrequest
COMMON = common utils charts
DEPS = babel-loader parse react github-api
STYLES = common compare style timeline pullrequest runset font-awesome.min

# Sources and targets

COMMON_SOURCES = $(addprefix src/,$(addsuffix .js,$(COMMON)))
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

build :
	@mkdir -p build

# Pages

define PAGE_RULE
build/$1.js : src/$1.js $(COMMON_SOURCES) $(DEP_TARGETS) Makefile webpack.config.js build
	@echo "Building page '$1'..."
	@webpack --devtool=inline-source-map src/$1.js --output-file build/$1.js

endef

$(foreach PAGE,$(PAGES),$(eval $(call PAGE_RULE,$(PAGE))))

# Dependencies

define DEP_RULE
node_modules/$1 :
	@echo 'Installing $1...'
	@npm install "$1"

endef

$(foreach DEP,$(DEPS),$(eval $(call DEP_RULE,$(DEP))))

# Styles

define STYLE_RULE
build/$1.css : src/$1.css Makefile build
	@echo "Building style '$1'..."
	@cp -f src/$1.css build/$1.css

endef

$(foreach STYLE,$(STYLES),$(eval $(call STYLE_RULE,$(STYLE))))

flow :
	( mv node_modules node_modules_save ; flow check ; ERR=$$? ; mv node_modules_save node_modules ; exit $$ERR )
