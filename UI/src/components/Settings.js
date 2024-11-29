import { toast } from 'react-toastify';
import { Nav, Tab } from 'react-bootstrap';
import { Container, Row, Col } from 'react-bootstrap';
import { useLocation, useNavigate } from 'react-router';
import React, { useState, useEffect, useCallback } from 'react';
import Scanner from './settings/Scanner';
import Mounts from './settings/Mounts';

export default function Settings() {
    const location = useLocation();
    const navigate = useNavigate();

    const getActiveKey = useCallback(() => location.pathname.match(/^\/settings\/(\w+)$/)?.[1], [location.pathname]);

    const [settings, setSettings] = useState(null);
    const [activeKey, setActiveKey] = useState(getActiveKey);

    // handle redirection on mount
    useEffect(() => {
        if (location.pathname === '/settings') {
            navigate('/settings/scanner', {replace: true});
        }
    }, [location.pathname, navigate]);

    // update the active key when the route changes
    useEffect(() => {
        const newActiveKey = getActiveKey();
        if (newActiveKey !== activeKey) {
            setActiveKey(newActiveKey);
        }
    }, [getActiveKey, activeKey]);

    // read the settings once on mount
    useEffect(() => {
        read();
    }, []);

    async function read() {
        try {
            const response = await fetch("/api/settings", {
                method: "GET",
                headers: {
                    accept: "application/json",
                }
            });
            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message ?? error.detail);
            }
            const data = await response.json();
            setSettings(data);
            return data;
        } catch (error) {
            console.error(error);
            toast.error(error.message);
            throw error;
        }
    }

    async function write(updates) {
        try {
            const response = await fetch("/api/settings", {
                method: "POST",
                headers: {
                    accept: "application/json",
                    "Content-Type": "application/json",
                },
                body: JSON.stringify(updates)
            })
            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message ?? error.detail);
            }
            return await read();
        } catch (error) {
            console.error(error);
            toast.error(error.message);
            throw error;
        }
    }

    // handle tab selection
    function onSelect(key) {
        setActiveKey(key);
        navigate(`/settings/${key}`);
    };

    return (
        <Container className="settings d-flex flex-column flex-grow-1 mb-3">
            <Tab.Container activeKey={activeKey} onSelect={onSelect}>
                <Row className="wrapper flex-md-grow-1">
                    <Col className="sidebar mb-3" sm={3}>
                        <Container className="title mb-4" fluid>
                            <h1>Settings</h1>
                        </Container>
                        <Nav variant="pills" className="flex-column">
                            <Nav.Item>
                                <Nav.Link eventKey="scanner"><i className="bi bi-binoculars me-3"></i>Media Scanner</Nav.Link>
                            </Nav.Item>
                            <Nav.Item>
                                <Nav.Link eventKey="mounts"><i className="bi bi-hdd-network me-3"></i>Network Mounts</Nav.Link>
                            </Nav.Item>
                        </Nav>
                    </Col>
                    <Col className="content" sm={9}>
                        <Tab.Content>
                            <Tab.Pane eventKey="scanner">
                                {activeKey === 'scanner' && <Scanner settings={settings} write={write}/>}
                            </Tab.Pane>
                            <Tab.Pane eventKey="mounts">
                                {activeKey === 'mounts' && <Mounts settings={settings} write={write}/>}
                            </Tab.Pane>
                        </Tab.Content>
                    </Col>
                </Row>
            </Tab.Container>
        </Container>
    );
};
