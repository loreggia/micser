import { Card } from "antd";
import React, { useEffect, useMemo, useState } from "react";
import styled from "styled-components";
import PageContainer from "../../PageContainer";
import WidgetPanel from "../../WidgetPanel";

const WidgetCard = styled(Card)``;

const Dashboard = () => {
    const [layout, setLayout] = useState([]);

    const widgets = useMemo(
        () => [
            {
                id: 1,
                header: "Widget 1",
                bounds: { top: 10, left: 10, width: 200, height: 100 },
            },
            {
                id: 2,
                header: "Widget 2",
                bounds: { top: 200, left: 40, width: 200, height: 200 },
            },
        ],
        []
    );

    useEffect(() => {
        const layout = widgets.map((w) => ({ id: w.id, bounds: w.bounds }));
        setLayout(layout);
    }, [widgets]);

    return (
        <PageContainer noPadding>
            <WidgetPanel layout={layout} onLayoutChanged={(l) => setLayout(l)}>
                {widgets.map((w) => (
                    <WidgetCard key={w.id}>{w.header}</WidgetCard>
                ))}
            </WidgetPanel>
        </PageContainer>
    );
};

export default Dashboard;
