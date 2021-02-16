const path = require("path");
const CracoLessPlugin = require("craco-less");

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

            const alias = webpackConfig.resolve.alias || {};
            alias["~"] = path.resolve(__dirname, "src/");
            webpackConfig.resolve.alias = alias;

            return webpackConfig;
        },
    },
};
