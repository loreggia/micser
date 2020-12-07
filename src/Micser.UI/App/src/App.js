import React, { Suspense } from "react";
import styled from "styled-components";
import { BrowserRouter as Router, Switch, Route } from "react-router-dom";
import { Layout } from "antd";

import { Dashboard, Navigation, NotFound } from "./components";

import { Routes } from "./utils/constants";

import "./i18n";
import "antd/dist/antd.css";
import "./App.scss";

const Loader = styled.div`
    width: 100%;
    min-height: 100vh;
    display: flex;
    justify-content: center;
    align-items: center;
`;

const MainContent = styled(Layout.Content)`
    padding: 20px;
`;

const App = () => {
    return (
        <Suspense fallback={<Loader>Loading...</Loader>}>
            <Router>
                <Layout style={{ minHeight: "100vh" }}>
                    <Navigation />

                    <Layout>
                        <MainContent>
                            <Switch>
                                <Route path={Routes.dashboard.index} exact>
                                    <Dashboard />
                                </Route>
                                {/* <Route path={Routes.settings.index}>
                            <Settings />
                        </Route> */}
                                <NotFound />
                            </Switch>
                        </MainContent>
                    </Layout>
                </Layout>
            </Router>
        </Suspense>
    );
};

export default App;
