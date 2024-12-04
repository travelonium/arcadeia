import cx from 'classnames';
import Form from 'react-bootstrap/Form';
import Card from 'react-bootstrap/Card';
import Button from 'react-bootstrap/Button';
import React, { useState, useEffect } from 'react';
import InputGroup from 'react-bootstrap/InputGroup';
import { Container, Row, Col } from 'react-bootstrap';

export default function Mounts({ settings, write }) {

    const [mounts, setMounts] = useState(null);

    const titles = {
        nfs: "(Network File System)",
        cifs: "SMB/CIFS (Windows Share)",
        sshfs: "SSHFS (SSH File System)"
    };

    useEffect(() => {
        setMounts(settings?.Mounts);
    }, [settings]);

    const Mount = ({ title, available, children }) => {
        return (
            <Row className="startup-scan align-items-center mb-3">
                <Card className="px-0">
                    <Card.Header className="pe-2">
                        <Row className="align-items-center">
                            <Col className="pe-0" xs="auto">
                                <i className={cx("bi bi-circle-fill",
                                        available ? "text-success" : "text-muted opacity-25"
                                )}></i>
                            </Col>
                            <Col><b>{title}</b></Col>
                            <Col xs="auto">
                                <Button variant="outline-secondary" size="sm" disabled>
                                    <i className="bi bi-dash-lg"></i>
                                </Button>
                            </Col>
                        </Row>
                    </Card.Header>
                    <Card.Body>{children}</Card.Body>
                </Card>
            </Row>
        );
    };

    function CIFS(props) {
        const folder = props.mount?.Folder;
        const device = props.mount?.Device;
        const options = props.mount?.Options;
        return (
            <>
                <InputGroup size="sm" className="mb-3">
                    <InputGroup.Text id="inputGroup-sizing-sm">Folder</InputGroup.Text>
                    <Form.Control aria-label="Folder" aria-describedby="inputGroup-sizing-sm" defaultValue={folder} disabled/>
                </InputGroup>
                <InputGroup size="sm" className="mb-3">
                    <InputGroup.Text id="inputGroup-sizing-sm">Device</InputGroup.Text>
                    <Form.Control aria-label="Device" aria-describedby="inputGroup-sizing-sm" defaultValue={device} disabled/>
                </InputGroup>
                <InputGroup size="sm" className="">
                    <InputGroup.Text id="inputGroup-sizing-sm">Options</InputGroup.Text>
                    <Form.Control aria-label="Options" aria-describedby="inputGroup-sizing-sm" defaultValue={options} disabled/>
                </InputGroup>
            </>

        );
    }

    return (
        <Container>
            <Row className="align-items-center mb-2">
                <Col className="my-1">
                    <h2>Network Mounts</h2>
                </Col>
                <Col xs="auto">
                    <Button className="me-2" variant="info" size="sm" disabled><i className="bi bi-plus-lg"></i></Button>
                </Col>
            </Row>
            <Row>
                <Col>
                    <Container>
                        {
                            mounts?.map((mount, index) => {
                                const type = mount?.Types;
                                const title = titles[type];
                                const available = mount?.Available ?? false;
                                if (type === "cifs") return (
                                    <Mount key={index} title={title} available={available}>
                                        <CIFS mount={mount} />
                                    </Mount>
                                ); else if (type === "nfs") return (
                                    <></>
                                ); else if (type === "sshfs") return (
                                    <></>
                                ); else return (
                                    <></>
                                );
                            })
                        }
                    </Container>
                </Col>
            </Row>
        </Container>
    );
};
