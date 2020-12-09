import React, { Suspense } from "react";
import styled from "styled-components";
import { BrowserRouter as Router, Switch, Route } from "react-router-dom";
import { Layout, Spin } from "antd";

import { Dashboard, Navigation, NotFound, Settings } from "./components";

import { Routes } from "./utils/constants";

import "./i18n";
import "./App.less";

const Loader = styled.div`
    width: 100%;
    min-height: 100vh;
    display: flex;
    justify-content: center;
    align-items: center;
`;

const App = () => {
    return (
        <Suspense
            fallback={
                <Loader>
                    <Spin tip="Loading..." />
                </Loader>
            }
        >
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
