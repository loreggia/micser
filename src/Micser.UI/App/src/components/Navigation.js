import React from "react";
import styled from "styled-components";
import { NavLink, useLocation } from "react-router-dom";
import { Layout, Menu } from "antd";
import { DashboardOutlined, SettingOutlined } from "@ant-design/icons";

const Brand = styled.div`
    margin: 20px;
    height: 32px;
    color: white;
`;

const Navigation = ({ className }) => {
    const location = useLocation();
    return (
        <Layout.Sider collapsible className={className}>
            <Brand>
                <NavLink to="/">Micser</NavLink>
            </Brand>
            <Menu theme="dark" mode="inline" selectedKeys={[location.pathname]}>
                <Menu.Item key="/" icon={<DashboardOutlined />}>
                    <NavLink to="/">Dashboard</NavLink>
                </Menu.Item>
                <Menu.Item key="/settings" icon={<SettingOutlined />}>
                    <NavLink to="/settings">Settings</NavLink>
                </Menu.Item>
            </Menu>
        </Layout.Sider>
    );
};

export default Navigation;