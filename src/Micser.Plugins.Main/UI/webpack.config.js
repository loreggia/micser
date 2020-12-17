const path = require("path");

module.exports = {
  entry: "./src/index.js",
  output: {
    path: path.resolve(__dirname, "../wwwroot"),
    filename: "plugin.js",
    library: "micser-plugins-main",
    libraryTarget: "umd",
    globalObject: "this",
  },
  module: {
    rules: [
      {
        test: /\.js$/,
        loader: "babel-loader",
        options: {
          presets: ["@babel/preset-env", "@babel/preset-react"],
        },
        exclude: /node_modules/,
        include: [path.resolve(__dirname, "src")],
      },
    ],
  },
  externals: {
    react: "react",
    "react-dom": "react-dom",
    "react-flow-renderer": "react-flow-renderer",
  },
};
