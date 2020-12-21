import React from "react";
import Loader from "./index.js";
import { withKnobs, text } from "@storybook/addon-knobs";

export default { title: "Loader", decorators: [withKnobs] };

export const main = () => {
    const tip = text("Tip", "Loading...");
    return <Loader tip={tip}></Loader>;
};
