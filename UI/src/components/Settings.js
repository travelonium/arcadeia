import { toast } from 'react-toastify';
import Mounts from './settings/Mounts';
import Scanner from './settings/Scanner';
import { Nav, Tab } from 'react-bootstrap';
import Transcoding from './settings/Transcoding';
import { Container, Row, Col } from 'react-bootstrap';
import { useLocation, useNavigate } from 'react-router';
import React, { useState, useEffect, useCallback } from 'react';

export default function Settings() {
    const location = useLocation();
    const navigate = useNavigate();

    const getActiveKey = useCallback(() => location.pathname.match(/^\/settings\/(\w+)$/)?.[1], [location.pathname]);

    const [settings, setSettings] = useState(null);
    const [activeKey, setActiveKey] = useState(getActiveKey);

    // handle redirection on mount
    useEffect(() => {
        if (location.pathname === '/settings') {
            const scanner = encodeURI("/settings/scanner" + location.search);
            navigate(scanner, {replace: true});
        }
    }, [location.pathname, location.search, navigate]);

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
        const url = encodeURI(`/settings/${key}` + location.search);
        navigate(url);
    };

    return (
        <Container className="settings d-flex flex-grow-1 overflow-auto" fluid>
            <Container className="wrapper d-flex flex-grow-1">
                <Tab.Container activeKey={activeKey} onSelect={onSelect}>
                    <Row className="flex-md-grow-1">
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
                                <Nav.Item>
                                    <Nav.Link eventKey="transcoding"><i className="bi bi-arrow-repeat me-3"></i>Transcoding</Nav.Link>
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
                                <Tab.Pane eventKey="transcoding">
                                    {activeKey === 'transcoding' && <Transcoding settings={settings} write={write}/>}
                                </Tab.Pane>
                            </Tab.Content>
                        </Col>
                    </Row>
                </Tab.Container>
            </Container>
        </Container>
    );
};
