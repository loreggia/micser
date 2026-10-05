// The widget bundle of the test plugin package (see globalSetup.ts). "react" comes from the UI through its import map, as in a real
// plugin's bundle (definePluginBuild keeps the shared modules external).
import { createElement } from "react";

function TestWidget({ module, setState }) {
  return createElement(
    "button",
    { type: "button", onClick: () => setState({ value: module.state.value + 1 }) },
    `Value ${module.state.value}`
  );
}

export default {
  name: "Test plugin",
  widgets: [{ moduleType: "Test", title: "Test module", component: TestWidget }],
};
