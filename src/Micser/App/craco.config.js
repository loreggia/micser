const CracoLessPlugin = require("craco-less");
const path = require("path");

module.exports = {
    plugins: [
        {
            plugin: CracoLessPlugin,
            options: {
                lessLoaderOptions: {
                    lessOptions: {
                        javascriptEnabled: true,
                    },
                },
            },
        },
    ],
    webpack: {
        configure: (webpackConfig) => {
            const scopePluginIndex = webpackConfig.resolve.plugins.findIndex(
                ({ constructor }) => constructor && constructor.name === "ModuleScopePlugin"
            );

            webpackConfig.resolve.plugins.splice(scopePluginIndex, 1);

            webpackConfig.resolve.alias = {
                "": path.resolve(__dirname, "./src/"),
            };

            return webpackConfig;
        },
    },
};
