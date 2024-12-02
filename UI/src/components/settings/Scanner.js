import Form from 'react-bootstrap/Form';
import Card from 'react-bootstrap/Card';
import Badge from 'react-bootstrap/Badge';
import Button from 'react-bootstrap/Button';
import React, { useState, useEffect } from 'react';
import { Container, Row, Col } from 'react-bootstrap';
import ToggleButton from 'react-bootstrap/ToggleButton';
import ToggleButtonGroup from 'react-bootstrap/ToggleButtonGroup';

export default function Scanner({ settings, write }) {

    const [startupScan, setStartupScan] = useState(null);
    const [startupUpdate, setStartupUpdate] = useState(null);
    const [forceGenerateMissingThumbnails, setForceGenerateMissingThumbnails] = useState(null);
    const [periodicScanIntervalMilliseconds, setPeriodicScanIntervalMilliseconds] = useState(null);

    useEffect(() => {
        setStartupScan(settings?.Scanner?.StartupScan);
        setStartupUpdate(settings?.Scanner?.StartupUpdate);
        setForceGenerateMissingThumbnails(settings?.Scanner?.ForceGenerateMissingThumbnails);
        setPeriodicScanIntervalMilliseconds(settings?.Scanner?.PeriodicScanIntervalMilliseconds);
    }, [settings]);

    function onChange(setting, value) {
        switch (setting) {
            case 'StartupScan':
                setStartupScan(value);
                write({ Scanner: { StartupScan: value } });
                break;

            case 'StartupUpdate':
                setStartupUpdate(value);
                write({ Scanner: { StartupUpdate: value } });
                break;

            case 'ForceGenerateMissingThumbnails':
                setForceGenerateMissingThumbnails(value);
                write({ Scanner: { ForceGenerateMissingThumbnails: value } });
                break;

            case 'PeriodicScanIntervalMilliseconds':
                setPeriodicScanIntervalMilliseconds(value * 60 * 60 * 1000);
                write({ Scanner: { PeriodicScanIntervalMilliseconds: value * 60 * 60 * 1000 } });
                break;

            default:
                break;
        }
    }

    return (
        <Container >
            <Row>
                <Col className="my-1 mb-3">
                    <h2>Media Scanner</h2>
                </Col>
            </Row>
            <Row>
                <Col>
                    <Container>
                        <Row className="startup-scan align-items-center mb-3">
                            <Card className="px-0">
                                <Card.Header className="pe-2">
                                    <Row className="align-items-center">
                                        <Col><b>Startup Scan</b></Col>
                                        <Col xs="auto">
                                            <Button className="shadow-none me-2" variant="outline-info" size="sm" disabled>Start Now</Button>
                                            <ToggleButtonGroup name="startup-scan" value={startupScan ? 1 : 0} onChange={(value) => onChange('StartupScan', value > 0)}>
                                                <ToggleButton type="radio" size="sm" id="startup-scan-off" variant="outline-secondary" name="radio" disabled={startupScan == null} value={0}>OFF</ToggleButton>
                                                <ToggleButton type="radio" size="sm" id="startup-scan-on" variant="outline-info" name="radio" disabled={startupScan == null} value={1}>ON</ToggleButton>
                                            </ToggleButtonGroup>
                                        </Col>
                                    </Row>
                                </Card.Header>
                                <Card.Body>
                                    <Card.Text className="d-flex align-items-center">
                                        <i className="bi bi-question-circle text-info pe-2"></i>
                                        Scan the watched, mounted and added folders for new media files at startup or manually.
                                    </Card.Text>
                                </Card.Body>
                            </Card>
                        </Row>

                        <Row className="startup-update align-items-center mb-3">
                            <Card className="px-0">
                                <Card.Header className="pe-2">
                                    <Row className="align-items-center">
                                        <Col><b>Startup Update</b></Col>
                                        <Col xs="auto">
                                            <Button className="shadow-none me-2" variant="outline-info" size="sm" disabled>Start Now</Button>
                                            <ToggleButtonGroup name="startup-update" value={startupUpdate ? 1 : 0} onChange={(value) => onChange('StartupUpdate', value > 0)}>
                                                <ToggleButton type="radio" size="sm" id="startup-update-off" variant="outline-secondary" name="radio" value={0} disabled={startupScan == null}>OFF</ToggleButton>
                                                <ToggleButton type="radio" size="sm" id="startup-update-on" variant="outline-info" name="radio" value={1} disabled={startupScan == null}>ON</ToggleButton>
                                            </ToggleButtonGroup>
                                        </Col>
                                    </Row>
                                </Card.Header>
                                <Card.Body>
                                    <Card.Text className="d-flex align-items-center">
                                        <i className="bi bi-question-circle text-info pe-2"></i>
                                        Go through the library and update modified items and remove the deleted ones at startup or manually.
                                    </Card.Text>
                                </Card.Body>
                            </Card>
                        </Row>

                        <Row className="force-generate-missing-thumbnails align-items-center mb-3">
                            <Card className="px-0">
                                <Card.Header className="pe-2">
                                    <Row className="align-items-center">
                                        <Col><b>Force Generate Missing Thumbnails</b></Col>
                                        <Col xs="auto">
                                            <ToggleButtonGroup name="force-generate-missing-thumbnails" value={forceGenerateMissingThumbnails ? 1 : 0} onChange={(value) => onChange('ForceGenerateMissingThumbnails', value > 0)}>
                                                <ToggleButton type="radio" size="sm" id="force-generate-missing-thumbnails-off" variant="outline-secondary" name="radio" disabled={forceGenerateMissingThumbnails == null} value={0}>OFF</ToggleButton>
                                                <ToggleButton type="radio" size="sm" id="force-generate-missing-thumbnails-on" variant="outline-info" name="radio" disabled={forceGenerateMissingThumbnails == null} value={1}>ON</ToggleButton>
                                            </ToggleButtonGroup>
                                        </Col>
                                    </Row>
                                </Card.Header>
                                <Card.Body>
                                    <Card.Text className="d-flex align-items-center">
                                        <i className="bi bi-question-circle text-info pe-2"></i>
                                        Regenerate any missing thumbnails upon access, scan or update.
                                    </Card.Text>
                                </Card.Body>
                            </Card>
                        </Row>

                        <Row className="periodic-scan-interval align-items-center mb-3">
                            <Card className="px-0">
                                <Card.Header className="pe-2">
                                    <Row className="align-items-center">
                                        <Col><b>Periodic Scan Interval</b></Col>
                                        <Col className="d-flex" xs="auto">
                                            <Badge bg={periodicScanIntervalMilliseconds > 0 ? "info" : "secondary"}>
                                                {
                                                    periodicScanIntervalMilliseconds > 0 ?
                                                        <>
                                                            <span className="periodic-scan-interval-value">{periodicScanIntervalMilliseconds != null ? Math.floor(periodicScanIntervalMilliseconds / (1000 * 60 * 60)) : 0}</span>
                                                            <span className="periodic-scan-interval-unit ms-1">{periodicScanIntervalMilliseconds > 60 * 60 * 1000 ? "HOURS" : "HOUR"}</span>
                                                        </> :
                                                        <span className="periodic-scan-interval-value">OFF</span>
                                                }
                                            </Badge>
                                        </Col>
                                        <Col className="d-flex mt-3 mt-sm-0" xs={12} sm="auto">
                                            <Form.Range min={0} max={168} step={1}
                                                name="periodic-scan-interval"
                                                disabled={periodicScanIntervalMilliseconds == null}
                                                value={periodicScanIntervalMilliseconds != null ? Math.floor(periodicScanIntervalMilliseconds / (1000 * 60 * 60)) : 0}
                                                onChange={(event) => setPeriodicScanIntervalMilliseconds(event.target.value * 60 * 60 * 1000)}
                                                onMouseUp={(event) => onChange('PeriodicScanIntervalMilliseconds', event.target.value)}
                                                onTouchEnd={(event) => onChange('PeriodicScanIntervalMilliseconds', event.target.value)}
                                                onKeyUp={(event) => onChange('PeriodicScanIntervalMilliseconds', event.target.value)} />
                                        </Col>
                                    </Row>
                                </Card.Header>
                                <Card.Body>
                                    <Card.Text className="d-flex align-items-center">
                                        <i className="bi bi-question-circle text-info pe-2"></i>
                                        Adjusts the interval at which a periodic scan is performed or disables it altogether.
                                    </Card.Text>
                                </Card.Body>
                            </Card>
                        </Row>

                    </Container>
                </Col>
            </Row>
        </Container>
    );
};
