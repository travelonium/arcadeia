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

import cx from 'classnames';
import Form from 'react-bootstrap/Form';
import Card from 'react-bootstrap/Card';
import { useState, useEffect, useRef } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { writeSettings } from '../../features/settings/slice';
import { Container, Row, Col, ListGroup, Spinner, Badge } from 'react-bootstrap';

export default function Logging() {
    const dispatch = useDispatch();

    const descriptions = {
        "Default": "This is the fallback log level. Any component not explicitly configured will inherit this level.",
        "Microsoft": "Controls the verbosity of logs from the .NET framework and Microsoft libraries.",
        "Microsoft.AspNetCore.SpaProxy": " Logs related to proxying requests to a frontend dev server (like React, Angular, or Vue).",
        "Microsoft.Hosting.Lifetime": "Logs application lifecycle events such as starting, stopping, and shutdown timing.",
    };

    const settings = useSelector((state) => state.settings.current);
    const readOnly = useSelector((state) => state.settings.current?.Security?.Settings?.ReadOnly);

    const [logs, setLogs] = useState([]);
    const [logLevel, setLogLevel] = useState(null);
    const [autoScroll, setAutoScroll] = useState(true);
    const recentLogsRef = useRef(null);

    useEffect(() => {
        setLogLevel(settings?.Logging?.LogLevel);
        const eventSource = new EventSource("/api/logs");
        eventSource.onmessage = (event) => {
            setLogs((prevLogs) => [...prevLogs.slice(-500), event.data]); // keep only the last 500 entries
            console.debug(event.data);
        };
        eventSource.onerror = (error) => {
            console.error(error);
            eventSource.close();
        };
        return () => {
            eventSource.close();
        };
    }, [settings]);

    useEffect(() => {
        if (autoScroll && recentLogsRef.current) {
            const el = recentLogsRef.current;
            el.scrollTop = el.scrollHeight;
        }
    }, [logs, autoScroll]);

    function onScroll() {
        if (!recentLogsRef.current) return;
        const el = recentLogsRef.current;
        const isAtBottom = el.scrollHeight - el.scrollTop <= el.clientHeight + 5;
        setAutoScroll(isAtBottom);
    }

    function getBadgeVariant(level) {
        switch (level) {
            case "Trace":
            case "Debug":
                return "dark";
            case "Information":
                return "info";
            case "Warning":
                return "warning";
            case "Error":
                return "danger";
            case "Critical":
                return "danger";
            default:
                return "secondary";
        }
    }

    function onChange(setting, value) {
        switch (setting) {
            case 'LogLevel':
                setLogLevel(value);
                dispatch(writeSettings({ Logging: { LogLevel: value ? value : {} } }));
                break;

            default:
                break;
        }
    }

    return (
        <Container className="logging">
            <Row className="align-items-center mb-2">
                <Col className="my-1">
                    <h2>Logging</h2>
                </Col>
            </Row>
            <Row>
                <Col>
                    <Container>
                        {
                            Object.keys(logLevel ?? {}).map((key, index) => (
                                <Row key={index} className={cx(key.toLowerCase().replace(/\./g, '-'), "align-items-center mb-3")}>
                                    <Card className="px-0">
                                        <Card.Header className="pe-2">
                                            <Row className="align-items-center">
                                                <Col><b>{key}</b></Col>
                                                <Col xs="auto">
                                                    <Form.Select size="sm" value={logLevel[key] ?? ""} onChange={(event) => onChange('LogLevel', { [key]: event.currentTarget.value })} disabled={readOnly}>
                                                        <option value="Trace">Trace</option>
                                                        <option value="Debug">Debug</option>
                                                        <option value="Information">Information</option>
                                                        <option value="Warning">Warning</option>
                                                        <option value="Error">Error</option>
                                                        <option value="Critical">Critical</option>
                                                        <option value="None">None</option>
                                                    </Form.Select>
                                                </Col>
                                            </Row>
                                        </Card.Header>
                                        <Card.Body>
                                            <Card.Text className="d-flex align-items-center">
                                                <i className="bi bi-question-circle text-info pe-2"></i>
                                                {descriptions[key]}
                                            </Card.Text>
                                        </Card.Body>
                                    </Card>
                                </Row>
                            ))
                        }

                        <Row className="recent-logs align-items-center mb-3">
                            <Card className="px-0">
                                <Card.Header className="pe-2">
                                    <Row className="align-items-center">
                                        <Col><b>Recent Logs</b></Col>
                                    </Row>
                                </Card.Header>
                                <Card.Body ref={recentLogsRef} onScroll={onScroll} className="font-monospace">
                                    <ListGroup variant="flush">
                                        {
                                            logs.length === 0 && (
                                                <div className="d-flex justify-content-center my-2">
                                                    <Spinner animation="border" size="sm" />
                                                </div>
                                            )
                                        }
                                        {
                                            logs.map((log, index) => {
                                                const match = log.match(/^\[(.*?)\]\s+(\S+)\s+\[(.*?)\]\s+(.*)$/);
                                                const [_, timestamp, category, level, message] = match || [];

                                                return (
                                                    <ListGroup.Item key={index}>
                                                    {
                                                        match ? (
                                                            <>
                                                                <Badge bg="secondary" className="me-2 align-middle">{timestamp}</Badge>
                                                                <Badge bg="primary" className="me-2 align-middle">{category}</Badge>
                                                                <Badge bg={getBadgeVariant(level)} className="me-2 align-middle">{level}</Badge>
                                                                <span className="align-middle">{message}</span>
                                                            </>
                                                        ) : (
                                                            log // fallback for logs that don't match the expected format
                                                        )
                                                    }
                                                    </ListGroup.Item>
                                                );
                                            })}
                                    </ListGroup>
                                </Card.Body>
                            </Card>
                        </Row>

                    </Container>
                </Col>
            </Row>
        </Container>
    );
};
