var webpack = require ('webpack');

var plugins;
if (!process.env ['DEV_SERVER']) {
    plugins = [
        new webpack.DefinePlugin({
            'process.env': {
                'NODE_ENV': '"production"'
            }
        })
    ];
}

module.exports = {
    entry: {
        compare: "./src/compare.tsx",
        config: "./src/config.tsx",
        machine: "./src/machine.tsx",
        timeline: "./src/timeline.tsx",
        runset: "./src/runset.tsx",
        pullrequest: "./src/pullrequest.tsx",
        pullrequests: "./src/pullrequests.tsx"
    },
    output: {
        filename: "./build/[name].js"
    },
    module: {
        loaders: [
            { test: /\.tsx?$/,
    		  exclude: /node_modules/,
    		  loader: 'ts-loader',
    		},
        ]
    },
    plugins: plugins
};
