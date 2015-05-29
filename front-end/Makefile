all : build/main.js

build/main.js : src/main.js Makefile
	jstransform --strip-types --react --harmony src/ build/
