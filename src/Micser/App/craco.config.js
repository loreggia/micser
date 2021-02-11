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

            return webpackConfig;
        },
    },
};
