module.exports = {
    entry: {
		compare: "./src/compare.js",
		config: "./src/config.js",
		timeline: "./src/timeline.js"
    },
    output: {
        path: __dirname + '/build',
        filename: "[name].js"
    },
    module: {
        loaders: [
            { test: /\.js$/,
			  exclude: /node_modules/,
			  loader: 'babel',
			},
        ]
    },
};
