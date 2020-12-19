const path = require("path");

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
                // include: [path.resolve(__dirname, "src")],
                exclude: /node_modules/,
            },
        ],
    },
    externals: {
        react: "React",
        "react-dom": "ReactDOM",
        antd: "Antd",
        "styled-components": "StyledComponents",
    },
};
