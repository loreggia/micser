import { createContext, useEffect, useState } from "react";
import load from "little-loader";
import { useGet } from "react-rest-api";
import { WidgetType } from "components/Pages/Dashboard/Widget";

const wnd = window as any;
wnd.process = { env: {} };

export const PluginsContext = createContext<Plugin[]>([]);

interface Plugin {
    name: string;
    widgets?: WidgetType[];
}
interface Module {
    default: () => Plugin;
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

export const usePlugins = () => {
    const { result: pluginDefinitions, loading: isLoadingApi } = useGet("/plugins");
    const [plugins, setPlugins] = useState<Plugin[]>([]);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        let isMounted = true;

        const loadPluginsAsync = async () => {
            setIsLoading(true);

            const result: Plugin[] = [];

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
