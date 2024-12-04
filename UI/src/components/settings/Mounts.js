import cx from 'classnames';
import Tab from 'react-bootstrap/Tab';
import Tabs from 'react-bootstrap/Tabs';
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
        const server = device.replace(/\/\//, "");
        const fields = { username: "", password: "", version: "" };
        options?.split(',').forEach(pair => {
            const [key, value] = pair.split('=');
            if (key === "username") fields.username = value;
            else if (key === "password") fields.password = value;
            else if (key === "vers") fields.version = value;
        });
        return (
            <Tabs
                defaultActiveKey="basic"
                id="type"
                className="mb-3"
                variant="tabs"
                justify
            >
                <Tab eventKey="basic" title="Basic" className="mb-2">
                    <Container>
                        <Row>
                            <Col>
                                <Form.Label htmlFor="folder">Folder</Form.Label>
                                <InputGroup id="folder" className="mb-3">
                                    <Form.Control aria-label="Folder" aria-describedby="folder" defaultValue={folder} disabled />
                                </InputGroup>
                            </Col>
                        </Row>
                        <Row>
                            <Col>
                                <Form.Label htmlFor="server">Server</Form.Label>
                                <InputGroup id="server" className="mb-3">
                                    <InputGroup.Text>//</InputGroup.Text>
                                    <Form.Control aria-label="Server" aria-describedby="server" defaultValue={server} disabled />
                                </InputGroup>
                            </Col>
                            <Col>
                                <Form.Label id="protocolVersionLabel" htmlFor="protocolVersion">Protocol Version</Form.Label>
                                <Form.Select id="protocolVersion" aria-label="Protocol Version" aria-describedby="protocolVersionLabel" defaultValue={fields.version} disabled>
                                    <option value="default" title="Tries to negotiate the highest SMB2+ version supported by both the client and server.">default</option>
                                    <option value="1.0" title="The classic CIFS/SMBv1 protocol.">1.0</option>
                                    <option value="2.0" title="The SMBv2.002 protocol. This was initially introduced in Windows Vista Service Pack 1, and Windows Server 2008. Note that the initial release version of Windows Vista spoke a slightly different dialect (2.000) that is not supported.">2.0</option>
                                    <option value="2.1" title="The SMBv2.1 protocol that was introduced in Microsoft Windows 7 and Windows Server 2008R2.">2.1</option>
                                    <option value="3.0" title="The SMBv3.0 protocol that was introduced in Microsoft Windows 8 and Windows Server 2012.">3.0</option>
                                    <option value="3.0.2" title="The SMBv3.0.2 protocol that was introduced in Microsoft Windows 8.1 and Windows Server 2012R2.">3.0.2</option>
                                    <option value="3.1.1" title="The SMBv3.1.1 protocol that was introduced in Microsoft Windows 10 and Windows Server 2016.">3.1.1</option>
                                    <option value="3" title="The SMBv3.0 protocol version and above.">3</option>
                                </Form.Select>
                            </Col>
                        </Row>
                        <Row>
                            <Col>
                                <Form.Label htmlFor="username">User Name</Form.Label>
                                <Form.Control
                                    id="username"
                                    defaultValue={fields.username}
                                    disabled
                                />

                            </Col>
                            <Col>
                                <Form.Label id="passwordLabel" htmlFor="password">Password</Form.Label>
                                <Form.Control
                                    type="password"
                                    id="password"
                                    aria-describedby="passwordLabel"
                                    defaultValue={fields.password}
                                    disabled
                                />
                            </Col>
                        </Row>
                    </Container>
                </Tab>
                <Tab eventKey="advanced" title="Advanced" className="mb-2">
                    <Container>
                        <Row>
                            <Col>
                                <Form.Label id="folderLabel" htmlFor="folder">Folder</Form.Label>
                                <InputGroup className="mb-3">
                                    <Form.Control aria-label="Folder" aria-describedby="folderLabel" defaultValue={folder} disabled />
                                </InputGroup>
                            </Col>
                        </Row>
                        <Row>
                            <Col>
                                <Form.Label id="deviceLabel" htmlFor="folder">Device</Form.Label>
                                <InputGroup className="mb-3">
                                    <Form.Control aria-label="Device" aria-describedby="deviceLabel" defaultValue={device} disabled />
                                </InputGroup>
                            </Col>
                        </Row>
                        <Row>
                            <Col>
                                <Form.Label id="optionsLabel" htmlFor="folder">Options</Form.Label>
                                <InputGroup className="">
                                    <Form.Control aria-label="Options" aria-describedby="optionsLabel" defaultValue={options} disabled />
                                </InputGroup>
                            </Col>
                        </Row>
                    </Container>
                </Tab>
            </Tabs>

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
