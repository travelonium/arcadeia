import { Nav, Tab } from 'react-bootstrap';
import React, { useState, useEffect, useCallback } from 'react';
import { Container, Row, Col } from 'react-bootstrap';
import { useLocation, useNavigate } from 'react-router';
import { Profile, Account, Preferences } from './settings/SettingsSections.js';

const Settings = () => {
    const location = useLocation();
    const navigate = useNavigate();

    const getActiveKey = useCallback(() => location.pathname.match(/^\/settings\/(\w+)$/)?.[1], [location.pathname]);

    // state to manage active tab
    const [activeKey, setActiveKey] = useState(getActiveKey);

    // handle redirection on mount
    useEffect(() => {
        if (location.pathname === '/settings') {
            navigate('/settings/profile', { replace: true });
        }
    }, [location.pathname, navigate]);

    // update the active key when the route changes
    useEffect(() => {
        const newActiveKey = getActiveKey();
        if (newActiveKey !== activeKey) {
            setActiveKey(newActiveKey);
        }
    }, [getActiveKey, activeKey]);

    // handle tab selection
    const onSelect = (key) => {
        setActiveKey(key);
        navigate(`/settings/${key}`);
    };

    return (
        <Container className="settings d-flex flex-column flex-grow-1 mb-3" fluid>
            <Container className="title mb-3" fluid>
                <h1>Settings</h1>
            </Container>
            <Tab.Container activeKey={activeKey} onSelect={onSelect}>
                <Row className="wrapper flex-md-grow-1">
                    <Col className="sidebar" sm={3}>
                        <Nav variant="pills" className="flex-column">
                            <Nav.Item>
                                <Nav.Link eventKey="profile">Profile</Nav.Link>
                            </Nav.Item>
                            <Nav.Item>
                                <Nav.Link eventKey="account">Account</Nav.Link>
                            </Nav.Item>
                            <Nav.Item>
                                <Nav.Link eventKey="preferences">Preferences</Nav.Link>
                            </Nav.Item>
                        </Nav>
                    </Col>
                    <Col className="content" sm={9}>
                        <Tab.Content>
                            <Tab.Pane eventKey="profile">
                                {activeKey === 'profile' && <Profile />}
                            </Tab.Pane>
                            <Tab.Pane eventKey="account">
                                {activeKey === 'account' && <Account />}
                            </Tab.Pane>
                            <Tab.Pane eventKey="preferences">
                                {activeKey === 'preferences' && <Preferences />}
                            </Tab.Pane>
                        </Tab.Content>
                    </Col>
                </Row>
            </Tab.Container>
        </Container>
    );
};

export default Settings;
