import React from "react";
import { Handle } from "react-flow-renderer";

const DeviceInputWidget = ({ data }) => {
  return (
    <div>
      <div>{JSON.stringify(data.state)}</div>
      <Handle type="source" position="right" id="Connector01" />
    </div>
  );
};

export default DeviceInputWidget;
