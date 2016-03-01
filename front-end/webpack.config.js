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
        pullrequests: "./src/pullrequests.tsx",
        pausetimes: "./src/pausetimes.tsx"
    },
    output: {
        filename: "./build/[name].js"
    },
    module: {
        loaders: [
            {
                test: /\.tsx?$/,
                exclude: /node_modules/,
                loader: 'ts-loader',
            },
            {
                test: /\.less$/,
                loader: "style-loader!css-loader!less-loader"
            },
            {
                test: /\.woff(2)?(\?v=[0-9]\.[0-9]\.[0-9])?$/,
                loader: "url-loader?limit=10000&minetype=application/font-woff&name=./build/[name].[ext]?[hash]"
            },
            {
                test: /\.(ttf|eot|svg)(\?v=[0-9]\.[0-9]\.[0-9])?$/,
                loader: "file-loader?name=./build/[name].[ext]?[hash]"
            }
        ]
    },
    plugins: plugins,
    devtool: 'eval'
};
