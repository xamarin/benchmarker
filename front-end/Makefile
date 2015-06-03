JSX_SOURCES = $(wildcard src/*.js)
JS_TARGETS = $(addprefix build/, $(notdir $(JSX_SOURCES)))
CSS_SOURCES = $(wildcard src/*.css)
CSS_TARGETS = $(addprefix build/, $(notdir $(CSS_SOURCES)))

all : $(JS_TARGETS) $(CSS_TARGETS)

build/%.js : src/%.js Makefile
	babel --source-maps=inline "$<" -o "$@"

build/%.css : src/%.css Makefile
	cp -f "$<" "$@"

lint :
	eslint src
