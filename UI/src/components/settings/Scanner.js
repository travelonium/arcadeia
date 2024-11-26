import { Container, Row, Col } from 'react-bootstrap';
import ToggleButton from 'react-bootstrap/ToggleButton';
import React, { useState, useEffect } from 'react';
import ToggleButtonGroup from 'react-bootstrap/ToggleButtonGroup';

export default function Scanner({ settings, write }) {
    const [startupScan, setStartupScan] = useState(null);
    const [startupUpdate, setStartupUpdate] = useState(null);
    const [forceGenerateMissingThumbnails, setForceGenerateMissingThumbnails] = useState(null);

    useEffect(() => {
        setStartupScan(settings?.Scanner?.StartupScan);
        setStartupUpdate(settings?.Scanner?.StartupUpdate);
        setForceGenerateMissingThumbnails(settings?.Scanner?.ForceGenerateMissingThumbnails);
    }, [settings]);

    function onChange(setting, value) {
        switch (setting) {
            case 'StartupScan':
                setStartupScan(value);
                write({Scanner: {StartupScan: value}});
                break;

            case 'StartupUpdate':
                setStartupUpdate(value);
                write({Scanner: {StartupUpdate: value}});
                break;

            case 'ForceGenerateMissingThumbnails':
                setForceGenerateMissingThumbnails(value);
                write({Scanner: {ForceGenerateMissingThumbnails: value}});
                break;

            default:
                break;
        }
    }

    return (
        <Container >
            <Row>
                <Col className="my-1">
                    <h2>Media Scanner</h2>
                </Col>
            </Row>
            <Row>
                <Col>
                    <Container>
                        <Row className="align-items-center mb-3">
                            <Col>Startup Scan</Col>
                            <Col xs="auto">
                                <ToggleButtonGroup name="startup-scan" value={startupScan ? 1 : 0} onChange={(value) => onChange('StartupScan', value > 0)}>
                                    <ToggleButton type="radio" size="sm" id="startup-scan-off" variant="outline-info" name="radio" disabled={startupScan == null} value={0}>OFF</ToggleButton>
                                    <ToggleButton type="radio" size="sm" id="startup-scan-on" variant="outline-info" name="radio" disabled={startupScan == null} value={1}>ON</ToggleButton>
                                </ToggleButtonGroup>
                            </Col>
                        </Row>

                        <Row className="align-items-center mb-3">
                            <Col>Startup Update</Col>
                            <Col xs="auto">
                                <ToggleButtonGroup name="startup-update" value={startupUpdate ? 1 : 0} onChange={(value) => onChange('StartupUpdate', value > 0)}>
                                    <ToggleButton type="radio" size="sm" id="startup-update-off" variant="outline-info" name="radio" value={0} disabled={startupScan == null}>OFF</ToggleButton>
                                    <ToggleButton type="radio" size="sm" id="startup-update-on" variant="outline-info" name="radio" value={1} disabled={startupScan == null}>ON</ToggleButton>
                                </ToggleButtonGroup>
                            </Col>
                        </Row>

                        <Row className="align-items-center">
                            <Col>Force Generate Missing Thumbnails</Col>
                            <Col xs="auto">
                                <ToggleButtonGroup name="force-generate-missing-thumbnails" value={forceGenerateMissingThumbnails ? 1 : 0} onChange={(value) => onChange('ForceGenerateMissingThumbnails', value > 0)}>
                                    <ToggleButton type="radio" size="sm" id="force-generate-missing-thumbnails-off" variant="outline-info" name="radio" disabled={forceGenerateMissingThumbnails == null} value={0}>OFF</ToggleButton>
                                    <ToggleButton type="radio" size="sm" id="force-generate-missing-thumbnails-on" variant="outline-info" name="radio" disabled={forceGenerateMissingThumbnails == null} value={1}>ON</ToggleButton>
                                </ToggleButtonGroup>
                            </Col>
                        </Row>

                    </Container>
                </Col>
            </Row>
        </Container>
    );
};
