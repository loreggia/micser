import React, { createContext, useEffect, useState } from "react";
import ReactDOM from "react-dom";
import load from "little-loader";

import useApi from "./useApi";

window.React = React;
window.ReactDOM = ReactDOM;
window.process = { env: {} };

export const PluginsContext = createContext([]);

const loadModuleAsync = (fileName, moduleName) => {
    return new Promise((resolve, reject) => {
        load(fileName, (err) => {
            if (err) {
                reject(err);
            } else {
                resolve(window[moduleName]);
            }
        });
    });
};

const usePlugins = () => {
    const [pluginsApi, isLoading] = useApi("plugins");
    const [plugins, setPlugins] = useState([]);

    useEffect(() => {
        const loadPlugins = async () => {
            const result = await pluginsApi();
            if (result.data) {
                const plugins = [];

                for (let i = 0; i < result.data.length; i++) {
                    let { assemblyName, moduleName } = result.data[i];
                    let fileName = `/_content/${assemblyName}/plugin.js`;
                    console.log("Loading plugin: " + fileName);
                    let module = await loadModuleAsync(fileName, moduleName);
                    let plugin = module.default();
                    plugins.push(plugin);
                }

                setPlugins(plugins);
            }
        };

        loadPlugins();
    }, [pluginsApi]);

    return [plugins, isLoading];
};

export default usePlugins;
