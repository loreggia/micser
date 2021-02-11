import React, { FC, memo, Suspense } from "react";
import { ApiProvider } from "react-rest-api";
import { BrowserRouter as Router, Switch, Route } from "react-router-dom";
import { Layout } from "antd";
import styled from "styled-components";

import * as antd from "antd";

import { Dashboard, Loader, Navigation, NotFound, Settings } from "components";

import { showError } from "./utils";
import { Routes } from "./utils/constants";
import { usePlugins } from "./hooks";
import { PluginsContext } from "./hooks/usePlugins";

import "./i18n";
import "./App.less";

const wnd = window as any;

wnd["react"] = React;
wnd["styled-components"] = styled;
wnd["antd"] = antd;

const App = ({ isLoading }: { isLoading: boolean }) => {
    const handleContextMenu = (e: React.MouseEvent) => {
        e.preventDefault();
    };

    return (
        <Router>
            <Layout style={{ minHeight: "100vh" }} onContextMenu={handleContextMenu}>
                <>
                    <Loader isVisible={isLoading} />

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
    );
};

const AppWithPlugins = () => {
    const [plugins, isLoading] = usePlugins();

    return (
        <PluginsContext.Provider value={plugins}>
            <App isLoading={isLoading} />
        </PluginsContext.Provider>
    );
};

const ApiConfig = {
    headers: {
        "Content-Type": "application/json",
    },
};

const AppWithApi = () => {
    const resolveCallback = async (response: Response) => {
        try {
            const body = await response.json();

            if (!response.ok) {
                showError(body || response);
            }

            return body;
        } catch (err) {
            showError(err);
            return null;
        }
    };

    const rejectCallback = async (response: Response) => {
        try {
            const body = await response.json();
            showError(body || response);
        } catch (err) {
            showError(err);
        }
    };

    return (
        <Suspense fallback={<Loader isVisible={true} />}>
            <ApiProvider
                url="/api"
                config={ApiConfig}
                resolveCallback={resolveCallback}
                rejectCallback={rejectCallback}
            >
                <AppWithPlugins />
            </ApiProvider>
        </Suspense>
    );
};

export default memo(AppWithApi);
