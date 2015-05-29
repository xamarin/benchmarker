all : build/main.js

build/main.js : src/main.js Makefile
	jstransform --strip-types --harmony src/ build/
