const path = require("path");
const nodeExternals = require("webpack-node-externals");

module.exports = {
    mode: "none",
    entry: "./src/index.ts",
    output: {
        path: path.resolve(__dirname, "../wwwroot"),
        filename: "plugin.js",
        library: "micser-plugins-main",
        libraryTarget: "umd",
        umdNamedDefine: true,
    },
    devtool: "source-map",
    resolve: {
        extensions: [".ts", ".tsx"],
    },
    module: {
        rules: [
            {
                test: /\.tsx?$/,
                loader: "ts-loader",
                exclude: /node_modules/,
            },
        ],
    },
    externals: [
        nodeExternals({
            importType: "umd",
        }),
    ],
};
