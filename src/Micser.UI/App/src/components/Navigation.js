import React from "react";
import { NavigationView, SplitViewCommand } from "react-uwp";

const Navigation = ({ children }) => {
    const navigationTopNodes = [<SplitViewCommand label="Dashboard" icon="Home" />];
    const navigationBottomNotes = [<SplitViewCommand label="Settings" icon="Settings" />];

    return (
        <NavigationView
            pageTitle="Micser"
            displayMode="compact"
            background="red"
            navigationTopNodes={navigationTopNodes}
            navigationBottomNodes={navigationBottomNotes}
        >
            {children}
        </NavigationView>
    );
};

export default Navigation;
