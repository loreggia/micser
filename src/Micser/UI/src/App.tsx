import React, { memo, Suspense } from "react";
import { BrowserRouter as Router, Switch, Route } from "react-router-dom";
import { Layout } from "antd";
import styled from "styled-components";

import * as antd from "antd";
import * as Common from "micser-common";

import { Navigation } from "~/components";
import { Dashboard, NotFound, Settings } from "~/pages";

import { Routes } from "~/constants";
import { usePlugins, PluginsContext } from "~/hooks";

import "./i18n";
import "./App.less";

const wnd = window as any;

wnd["react"] = React;
wnd["styled-components"] = styled;
wnd["antd"] = antd;
wnd["micser-common"] = Common;

const App = () => {
    const [plugins, isLoading] = usePlugins();

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
