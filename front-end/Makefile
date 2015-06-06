JS_TARGETS = build/compare.js build/config.js build/machine.js build/timeline.js

COMMON_SOURCES = src/utils.js src/common.js

CSS_SOURCES = $(wildcard src/*.css)
CSS_TARGETS = $(addprefix build/, $(notdir $(CSS_SOURCES)))

NODE_MODULES = node_modules/babel-loader node_modules/parse node_modules/react

all : $(JS_TARGETS) $(CSS_TARGETS) $(NODE_MODULES)

$(JS_TARGETS) : src/compare.js src/config.js src/machine.js src/timeline.js $(COMMON_SOURCES) Makefile
	webpack --devtool=inline-source-map

build/%.css : src/%.css Makefile
	cp -f "$<" "$@"

lint :
	eslint src

node_modules/babel-loader :
	npm install babel-loader

node_modules/parse :
	npm install parse

node_modules/react :
	npm install react

clean :
	rm -rf build/*
