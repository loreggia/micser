import { createContext, useEffect, useState } from "react";
import load from "little-loader";

import useApi from "./useApi";

export const PluginsContext = createContext([]);

const usePlugins = () => {
    const [pluginsApi, isLoading] = useApi("plugins");
    const [plugins, setPlugins] = useState([]);

    useEffect(() => {
        const loadPlugins = async () => {
            const result = await pluginsApi();
            if (result.data) {
                const promises = result.data.map((name) => {
                    const fileName = `/_content/${name}/plugin.js`;
                    console.log("Loading plugin: " + fileName);
                    return new Promise((resolve) => {
                        load(fileName, (err) => {
                            if (err) {
                                console.error(err);
                            }
                            resolve();
                        });
                    });
                });

                await Promise.all(promises);

                setPlugins(window.plugins || []);
            }
        };

        loadPlugins();
    }, [pluginsApi]);

    return [plugins, isLoading];
};

export default usePlugins;
