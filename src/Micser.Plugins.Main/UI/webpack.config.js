const path = require("path");
const nodeExternals = require("webpack-node-externals");

module.exports = {
    entry: "./src/index.js",
    output: {
        path: path.resolve(__dirname, "../wwwroot"),
        filename: "plugin.js",
        library: "micser-plugins-main",
        libraryTarget: "umd",
        umdNamedDefine: true,
    },
    devtool: "source-map",
    module: {
        rules: [
            {
                test: /\.js$/,
                loader: "babel-loader",
                options: {
                    presets: ["@babel/preset-env", "@babel/preset-react"],
                },
                include: [path.resolve(__dirname, "src")],
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
