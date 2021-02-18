import { createContext, useEffect, useState } from "react";
import load from "little-loader";
import { Plugin, PluginDefinition, useGetApi } from "micser-common";

const wnd = window as any;
wnd.process = { env: {} };

export const PluginsContext = createContext<Plugin[]>([]);

interface Module {
    default: Plugin;
}

const loadModuleAsync = (fileName: string, moduleName: string): Promise<Module> => {
    return new Promise((resolve, reject) => {
        load(fileName, (err) => {
            if (err) {
                reject(err);
            } else {
                resolve(wnd[moduleName]);
            }
        });
    });
};

export const usePlugins = (): [Plugin[], boolean] => {
    const [pluginDefinitions, { isLoading: isLoadingApi }] = useGetApi<PluginDefinition[]>("plugins");

    const [plugins, setPlugins] = useState<Plugin[]>([]);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        let isMounted = true;

        const loadPluginsAsync = async () => {
            setIsLoading(true);

            const result: Plugin[] = [];

            if (pluginDefinitions) {
                for (let i = 0; i < pluginDefinitions.length; i++) {
                    let { assemblyName, moduleName } = pluginDefinitions[i];
                    let fileName = `/_content/${assemblyName}/plugin.js`;

                    console.log("Loading plugin: " + fileName);
                    let module = await loadModuleAsync(fileName, moduleName);

                    if (!isMounted) {
                        return;
                    }

                    result.push(module.default);
                }
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
