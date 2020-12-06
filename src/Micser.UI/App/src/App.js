import React, { Suspense } from "react";
import { BrowserRouter as Router, Switch, Route } from "react-router-dom";
import { Theme as UWPThemeProvider, getTheme } from "react-uwp/Theme";
import { ProgressRing } from "react-uwp";
import styled from "styled-components";

import { Navigation, NotFound } from "./components";

import { Routes } from "./utils/constants";

import "./i18n";
import "./App.scss";

const Loader = styled.div`
    width: 100%;
    height: 100vh;
    display: flex;
    justify-content: center;
    align-items: center;
`;

const MainLayout = styled.div`
    display: flex;
    height: 100%;
`;

const theme = getTheme({
    themeName: "dark",
    accent: "#84f542",
});

const App = () => {
    return (
        <UWPThemeProvider theme={theme}>
            <Suspense
                fallback={
                    <Loader>
                        <ProgressRing />
                    </Loader>
                }
            >
                <Router>
                    <Navigation>
                        <Switch>
                            <Route path={Routes.dashboard.index} exact></Route>
                            <NotFound />
                        </Switch>
                    </Navigation>
                </Router>
            </Suspense>
        </UWPThemeProvider>
    );
};

export default App;
