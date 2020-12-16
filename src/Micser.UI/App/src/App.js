import React, { Suspense } from "react";
import { BrowserRouter as Router, Switch, Route } from "react-router-dom";
import { Layout } from "antd";

import { Dashboard, Loader, Navigation, NotFound, Settings } from "./components";

import { Routes } from "./utils/constants";

import "./i18n";
import "./App.less";

const App = () => {
    return (
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
    );
};

export default App;
