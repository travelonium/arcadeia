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

import Form from 'react-bootstrap/Form';
import Card from 'react-bootstrap/Card';
import React, { useState, useEffect } from 'react';
import { Container, Row, Col } from 'react-bootstrap';
import { useDispatch, useSelector } from 'react-redux';
import { writeSettings } from '../../features/settings/slice';

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

    const [logLevel, setLogLevel] = useState(null);

    useEffect(() => {
        setLogLevel(settings?.Logging?.LogLevel);
    }, [settings]);

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
        <Container>
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
                            <Row key={index} className="startup-scan align-items-center mb-3">
                                <Card className="px-0">
                                    <Card.Header className="pe-2">
                                        <Row className="align-items-center">
                                            <Col><b>{key}</b></Col>
                                            <Col xs="auto">
                                                <Form.Select size="sm" value={logLevel[key] ?? ""} onChange={(event) => onChange('LogLevel', {[key]: event.currentTarget.value})} disabled={readOnly}>
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
                    </Container>
                </Col>
            </Row>
        </Container>
    );
};
