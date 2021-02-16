import React from "react";
import { Loader } from "./Loader";
import { withKnobs, text, boolean } from "@storybook/addon-knobs";

export default { title: "Loader", decorators: [withKnobs] };

export const main = () => {
    const tip = text("Tip", "Loading...");
    const isVisible = boolean("IsVisible", true);
    return <Loader isVisible={isVisible} tip={tip} />;
};
