import { createContext, useEffect, useState } from "react";
// import ReactDOM from "react-dom";
// import * as antd from "antd";
// import styled from "styled-components";
import load from "little-loader";
import { useApi, useGet } from "react-rest-api";

// window.React = React;
// window.ReactDOM = ReactDOM;
// window.Antd = antd;
// window.StyledComponents = styled;
// window.MicserCommon = MicserCommon;
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
    const { result: pluginDefinitions, loading: isLoadingApi } = useGet("/plugins");
    const [plugins, setPlugins] = useState([]);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        let isMounted = true;

        const loadPluginsAsync = async () => {
            setIsLoading(true);

            const result = [];

            for (let i = 0; i < pluginDefinitions.length; i++) {
                let { assemblyName, moduleName } = pluginDefinitions[i];
                let fileName = `/_content/${assemblyName}/plugin.js`;

                console.log("Loading plugin: " + fileName);
                let module = await loadModuleAsync(fileName, moduleName);

                if (!isMounted) {
                    return;
                }

                let plugin = module.default();
                result.push(plugin);
            }

            setPlugins(result);
            setIsLoading(false);
        };

        if (pluginDefinitions) {
            loadPluginsAsync();
        }

        return () => {
            isMounted = false;
        };
    }, [pluginDefinitions]);

    return [plugins, isLoadingApi || isLoading];
};

export default usePlugins;
