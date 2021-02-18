module.exports = {
    stories: ["../src/components/**/*.stories.tsx"],
    addons: ["@storybook/addon-knobs/register", "@storybook/preset-ant-design"],
    typescript: {
        check: false,
        checkOptions: {},
        reactDocgen: "react-docgen-typescript",
        reactDocgenTypescriptOptions: {
            shouldExtractLiteralValuesFromEnum: true,
            propFilter: (prop) => (prop.parent ? !/node_modules/.test(prop.parent.fileName) : true),
        },
    },
};
