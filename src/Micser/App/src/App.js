import React, { Suspense } from "react";
import { BrowserRouter as Router, Switch, Route } from "react-router-dom";
import { Layout } from "antd";

import { Dashboard, Loader, Navigation, NotFound, Settings } from "./components";

import { Routes } from "./utils/constants";
import { usePlugins } from "./hooks";
import { PluginsContext } from "./hooks/usePlugins";

import "./i18n";
import "./App.less";

const App = () => {
    const [plugins, isLoading] = usePlugins();

    return isLoading ? (
        <Loader />
    ) : (
        <PluginsContext.Provider value={plugins}>
            <Suspense fallback={<Loader />}>
                <Router>
                    <Layout style={{ minHeight: "100vh" }}>
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
                    </Layout>
                </Router>
            </Suspense>
        </PluginsContext.Provider>
    );
};

export default App;
