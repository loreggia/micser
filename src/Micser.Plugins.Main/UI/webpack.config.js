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
    externals: [nodeExternals()],
    // externals: {
    //     react: {
    //         root: "React",
    //         commonjs2: "react",
    //         commonjs: "react",
    //         amd: "react",
    //         umd: "react",
    //     },
    //     "react-dom": {
    //         root: "ReactDOM",
    //         commonjs2: "react-dom",
    //         commonjs: "react-dom",
    //         amd: "react-dom",
    //         umd: "react-dom",
    //     },
    //     antd: {
    //         root: "Antd",
    //         commonjs2: "antd",
    //         commonjs: "antd",
    //         amd: "antd",
    //         umd: "antd",
    //     },
    //     "micser-common": {
    //         root: "MicserCommon",
    //         commonjs2: "micser-common",
    //         commonjs: "micser-common",
    //         amd: "micser-common",
    //         umd: "micser-common",
    //     },
    // },
};
