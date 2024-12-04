import cx from 'classnames';
import { clone } from '../../utils';
import Tab from 'react-bootstrap/Tab';
import { isEqual, omit } from 'lodash';
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

    const Mount = ({ index, mount, title, available, error, children }) => {

        const [state, setState] = useState(clone(mount));
        const [applyButtonState, setApplyButtonState] = useState(true);
        const [resetButtonState, setResetButtonState] = useState(true);

        useEffect(() => {
            if (isEqual(mount, state)) {
                setApplyButtonState(false);
                setResetButtonState(false);
            } else {
                setApplyButtonState(true);
                setResetButtonState(true);
            }
        }, [state, mount]);

        function reset() {
            setState(clone(mount))
        }

        function apply() {
            let updates = clone(mounts).map((item, i) =>
            {
                const { Types, Options, Device, Folder } = i === index ? state : item;
                return { Types, Options, Device, Folder };
            });
            setMounts(updates);
            write({ Mounts: updates });
        }

        return (
            <Row className="startup-scan align-items-center mb-3">
                <Card className="px-0">
                    <Card.Header className="pe-2">
                        <Row className="align-items-center">
                            <Col className="pe-0" xs="auto">
                                <i className={cx("bi bi-circle-fill", available ? "text-success" : error ? "text-danger" : "text-muted opacity-25")} title={error ? error : undefined} />
                            </Col>
                            <Col><b>{title}</b></Col>
                            <Col xs="auto">
                                <Button variant="outline-secondary" size="sm" disabled>
                                    <i className="bi bi-dash-lg"></i>
                                </Button>
                            </Col>
                        </Row>
                    </Card.Header>
                    <Card.Body>
                    {
                        React.Children.map(children, (child) => {
                            return React.isValidElement(child) ? React.cloneElement(child, { mount, state, setState }) : child;
                        })
                    }
                    </Card.Body>
                    <Card.Footer className="px-2">
                        <Container className="py-2" fluid>
                            <Row className="g-4 justify-content-center">
                                <Col className="">
                                    <Button className="reset" variant="danger" disabled={!resetButtonState} onClick={reset}>Reset</Button>
                                </Col>
                                <Col className="" xs="auto">
                                    <Button className="apply" variant="primary" disabled={!applyButtonState} onClick={apply}>Apply</Button>
                                </Col>
                            </Row>
                        </Container>
                    </Card.Footer>
                </Card>
            </Row>
        );
    };

    const CIFS = ({ mount, state, setState }) => {

        const folder = state.Folder;
        const device = state.Device;
        const options = state.Options;
        const server = device.replace(/\/\//, "");
        const fields = { username: "", password: "", vers: "" };

        options?.split(',').forEach(pair => {
            const [key, value] = pair.split('=');
            if (key === "username") fields.username = value;
            else if (key === "password") fields.password = value;
            else if (key === "vers") fields.vers = value;
        });

        function onFolderChange(event) {
            const value = event.target.value;
            setState({
                ...state,
                Folder: value
            });
        }

        function onServerChange(event) {
            const value = event.target.value;
            setState({
                ...state,
                Device: `//${value}`
            });
        }

        function onDeviceChange(event) {
            const value = event.target.value;
            setState({
                ...state,
                Device: value
            });
        }

        function onOptionsChange(event) {
            const value = event.target.value;
            setState({
                ...state,
                Options: value
            });
        }

        function onUserNameChange(event) {
            const value = event.target.value;
            fields.username = value;
            setState({
                ...state,
                Options: Object.entries(fields)
                               .filter(([_, value]) => value !== "")
                               .map(([key, value]) => `${key}=${value}`)
                               .join(",")
            });
        }

        function onPasswordChange(event) {
            const value = event.target.value;
            fields.password = value;
            setState({
                ...state,
                Options: Object.entries(fields)
                               .filter(([_, value]) => value !== "")
                               .map(([key, value]) => `${key}=${value}`)
                               .join(",")
            });
        }

        function onVersionChange(event) {
            const value = event.target.value;
            fields.vers = value;
            setState({
                ...state,
                Options: Object.entries(fields)
                               .filter(([key, value]) => (key === "vers" && value !== "default") || (key !== "vers" && value !== ""))
                               .map(([key, value]) => `${key}=${value}`)
                               .join(",")
            });
        }

        return (
            <Tabs
                defaultActiveKey="basic"
                id="type"
                className="mb-3"
                variant="pills"
                justify
            >
                <Tab eventKey="basic" title="Basic" className="mb-2">
                    <Container>
                        <Row>
                            <Col>
                                <Form.Label htmlFor="folder">Folder</Form.Label>
                                <InputGroup id="folder" className="mb-3">
                                    <Form.Control aria-label="Folder" aria-describedby="folder" value={folder} onChange={onFolderChange} />
                                </InputGroup>
                            </Col>
                        </Row>
                        <Row>
                            <Col>
                                <Form.Label htmlFor="server">Server</Form.Label>
                                <InputGroup id="server" className="mb-3">
                                    <InputGroup.Text>//</InputGroup.Text>
                                    <Form.Control aria-label="Server" aria-describedby="server" value={server} onChange={onServerChange} />
                                </InputGroup>
                            </Col>
                            <Col>
                                <Form.Label id="protocolVersionLabel" htmlFor="protocolVersion">Protocol Version</Form.Label>
                                <Form.Select id="protocolVersion" aria-label="Protocol Version" aria-describedby="protocolVersionLabel" value={fields.vers} onChange={onVersionChange} >
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
                                <Form.Control id="username" value={fields.username} onChange={onUserNameChange} />

                            </Col>
                            <Col>
                                <Form.Label id="passwordLabel" htmlFor="password">Password</Form.Label>
                                <Form.Control type="password" id="password" aria-describedby="passwordLabel" value={fields.password} onChange={onPasswordChange}/>
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
                                    <Form.Control aria-label="Folder" aria-describedby="folderLabel" value={folder} onChange={onFolderChange} />
                                </InputGroup>
                            </Col>
                        </Row>
                        <Row>
                            <Col>
                                <Form.Label id="deviceLabel" htmlFor="folder">Device</Form.Label>
                                <InputGroup className="mb-3">
                                    <Form.Control aria-label="Device" aria-describedby="deviceLabel" value={device} onChange={onDeviceChange} />
                                </InputGroup>
                            </Col>
                        </Row>
                        <Row>
                            <Col>
                                <Form.Label id="optionsLabel" htmlFor="folder">Options</Form.Label>
                                <InputGroup className="">
                                    <Form.Control aria-label="Options" aria-describedby="optionsLabel" value={options} onChange={onOptionsChange} />
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
                                const error = mount?.Error;
                                if (type === "cifs") return (
                                    <Mount key={index} index={index} mount={mount} title={title} available={available} error={error}>
                                        <CIFS />
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
