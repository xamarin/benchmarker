var webpack = require ('webpack');

module.exports = {
    entry: {
        compare: "./src/compare.js",
        config: "./src/config.js",
        machine: "./src/machine.js",
        timeline: "./src/timeline.js",
        runset: "./src/runset.js",
        pullrequest: "./src/pullrequest.js"
    },
    output: {
        filename: "./build/[name].js"
    },
    module: {
        loaders: [
            { test: /\.js$/,
    		  exclude: /node_modules/,
    		  loader: 'babel',
    		},
        ]
    },
    plugins: [
        new webpack.DefinePlugin({
            'process.env': {
                'NODE_ENV': '"production"'
            }
        })
    ]
};
