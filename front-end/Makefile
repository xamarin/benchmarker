all : build/main.js build/utils.js

build/main.js : src/main.js Makefile
	babel --source-maps=inline src/main.js -o build/main.js

build/utils.js : src/utils.js Makefile
	babel --source-maps=inline src/utils.js -o build/utils.js
