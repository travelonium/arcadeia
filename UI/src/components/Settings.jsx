/*
 *  Copyright © 2024 Travelonium AB
 *
 *  This file is part of Arcadeia.
 *
 *  Arcadeia is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Affero General Public License as published
 *  by the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Arcadeia is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU Affero General Public License for more details.
 *
 *  You should have received a copy of the GNU Affero General Public License
 *  along with Arcadeia. If not, see <https://www.gnu.org/licenses/>.
 *
 */

import { toast } from 'react-toastify';
import Mounts from './settings/Mounts';
import Scanner from './settings/Scanner';
import { Nav, Tab } from 'react-bootstrap';
import Transcoding from './settings/Transcoding';
import { Container, Row, Col } from 'react-bootstrap';
import { useDispatch, useSelector } from 'react-redux';
import { useLocation, useNavigate } from 'react-router';
import { readSettings } from '../features/settings/slice';
import React, { useState, useEffect, useCallback } from 'react';

export default function Settings() {
    const location = useLocation();
    const navigate = useNavigate();
    const dispatch = useDispatch();

    const getActiveKey = useCallback(() => location.pathname.match(/^\/settings\/(\w+)$/)?.[1], [location.pathname]);

    const { error } = useSelector((state) => state.settings);
    const settings = useSelector((state) => state.settings.current);

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
        dispatch(readSettings());
    }, [dispatch]);

    useEffect(() => {
        if (!error) return;
        console.error(error);
        toast.error(error);
    }, [error]);

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
                                    {activeKey === 'scanner' && <Scanner />}
                                </Tab.Pane>
                                <Tab.Pane eventKey="mounts">
                                    {activeKey === 'mounts' && <Mounts />}
                                </Tab.Pane>
                                <Tab.Pane eventKey="transcoding">
                                    {activeKey === 'transcoding' && <Transcoding />}
                                </Tab.Pane>
                            </Tab.Content>
                        </Col>
                    </Row>
                </Tab.Container>
            </Container>
        </Container>
    );
};
