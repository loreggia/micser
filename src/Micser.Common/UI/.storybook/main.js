module.exports = {
    stories: ["../src/components/**/*.stories.[tj]s"],
    addons: ["@storybook/addon-knobs/register", "@storybook/preset-ant-design"],
    webpackFinal: async (config) => {
        config.externals = [];

        return config;
    },
};
