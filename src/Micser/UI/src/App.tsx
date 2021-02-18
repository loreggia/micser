import React, { memo, Suspense, useEffect } from "react";
import { BrowserRouter as Router, Switch, Route } from "react-router-dom";
import { Layout } from "antd";
import styled from "styled-components";

import * as antd from "antd";
import * as ReactI18Next from "react-i18next";
import * as Common from "micser-common";

import { Navigation } from "~/components";
import { Dashboard, NotFound, Settings } from "~/pages";

import { Routes } from "~/constants";
import { usePlugins, PluginsContext } from "~/hooks";

import i18n from "./i18n";
import "./App.less";

const wnd = window as any;

wnd["react"] = React;
wnd["styled-components"] = styled;
wnd["antd"] = antd;
wnd["react-i18next"] = ReactI18Next;
wnd["micser-common"] = Common;

const App = () => {
    const [plugins, isLoading] = usePlugins();

    // load plugin resources
    useEffect(() => {
        plugins.forEach((plugin) => {
            if (plugin.resources) {
                for (const lng in plugin.resources) {
                    if (Object.prototype.hasOwnProperty.call(plugin.resources, lng)) {
                        const bundle = plugin.resources[lng];
                        i18n.addResourceBundle(lng, `plugins.${plugin.name}`, bundle, true, true);
                    }
                }
            }
        });
    }, [plugins]);

    const handleContextMenu = (e: React.MouseEvent) => {
        e.preventDefault();
    };

    return (
        <Suspense fallback={<Common.Loader isVisible={true} />}>
            <PluginsContext.Provider value={plugins}>
                <Router>
                    <Layout style={{ minHeight: "100vh" }} onContextMenu={handleContextMenu}>
                        <>
                            <Common.Loader isVisible={isLoading} />

                            <Navigation />

                            <Layout>
                                <Switch>
                                    <Route path={Routes.dashboard.index} exact>
                                        <Dashboard />
                                    </Route>
                                    <Route path={Routes.settings.index}>
                                        <Settings />
                                    </Route>
                                    <NotFound />
                                </Switch>
                            </Layout>
                        </>
                    </Layout>
                </Router>
            </PluginsContext.Provider>
        </Suspense>
    );
};

export default memo(App);
