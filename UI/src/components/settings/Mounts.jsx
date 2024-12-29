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
import { isEqual } from 'lodash';
import { clone } from '../../utils';
import Tab from 'react-bootstrap/Tab';
import Tabs from 'react-bootstrap/Tabs';
import Form from 'react-bootstrap/Form';
import Card from 'react-bootstrap/Card';
import Button from 'react-bootstrap/Button';
import Dropdown from 'react-bootstrap/Dropdown';
import React, { useState, useEffect } from 'react';
import InputGroup from 'react-bootstrap/InputGroup';
import { Container, Row, Col } from 'react-bootstrap';
import { useDispatch, useSelector } from 'react-redux';
import { writeSettings } from '../../features/settings/slice';

export default function Mounts() {
    const dispatch = useDispatch();

    const settings = useSelector((state) => state.settings.current);
    const readOnly = useSelector((state) => state.settings.current?.Security?.Settings?.ReadOnly);

    const [mounts, setMounts] = useState(null);

    const titles = {
        nfs: "NFS (Network File System)",
        cifs: "SMB/CIFS (Windows Share)",
        sshfs: "SSHFS (SSH File System)"
    };

    useEffect(() => {
        setMounts(settings?.Mounts);
    }, [settings]);

    function add(types) {
        setMounts([{ Types: types, Options: "", Device: "", Folder: "" }, ...clone(mounts ?? [])]);
    }

    const Mount = ({ mounts, index, mount, title, available, error, children }) => {

        const [state, setState] = useState(clone(mount));
        const [applyButtonState, setApplyButtonState] = useState(true);
        const [resetButtonState, setResetButtonState] = useState(true);

        useEffect(() => {
            function validate(mount) {
                const types = mount.Types;
                const device = mount.Device;
                const folder = mount.Folder;
                if (types === "cifs") {
                    if (!device) return false;
                    if (!folder) return false;
                    if (mounts.map(item => item.Folder).filter((item, i) => i !== index && item === folder).length > 0) return false;
                } else if (types === "nfs") {
                    if (!device) return false;
                    if (!/^((?:(?:\d{1,3}\.){3}\d{1,3})|(?:[a-zA-Z0-9-]+\.)*[a-zA-Z0-9-]+):\/(?:[^\s]+)$/.test(device)) return false;
                    if (!folder) return false;
                    if (mounts.map(item => item.Folder).filter((item, i) => i !== index && item === folder).length > 0) return false;
                } else {
                    return false;
                }
                return true;
            }
            if (!isEqual(mount, state)) {
                if (validate(state)) {
                    setApplyButtonState(true);
                } else {
                    setApplyButtonState(false);
                }
                setResetButtonState(true);
            } else {
                setApplyButtonState(false);
                setResetButtonState(false);
            }
        }, [state, mount, mounts, index]);

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
            dispatch(writeSettings({ Mounts: updates }));
        }

        function remove() {
            let updates = clone(mounts).filter((_, i) => i !== index);
            setMounts(updates);
            dispatch(writeSettings({ Mounts: updates }));
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
                                <Button variant="danger" size="sm" onClick={remove} disabled={readOnly}>
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
                                <Col className="d-flex me-auto" xs={6} sm="auto">
                                    <Button className="reset flex-grow-1" variant="danger" disabled={!resetButtonState || readOnly} onClick={reset}>Reset</Button>
                                </Col>
                                <Col className="d-flex" xs={6} sm="auto">
                                    <Button className="apply flex-grow-1" variant="primary" disabled={!applyButtonState || readOnly} onClick={apply}>Apply</Button>
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
        const fields = { username: "", password: "", vers: "" };

        options?.split(',').forEach(pair => {
            const [key, value] = pair.split('=');
            if (key === "username") fields.username = value;
            else if (key === "password") fields.password = value;
            else if (key === "vers") fields.vers = value;
        });

        const [showOptions, setShowOptions] = useState(false);

        function onFolderChange(event) {
            const value = event.target.value;
            setState({
                ...state,
                Folder: value ? `/Network/${value}` : ""
            });
        }

        function onAdvancedFolderChange(event) {
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
                Device: value ? `//${value}` : ""
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
                <Tab eventKey="basic" title="Basic" className="mb-2" disabled={readOnly}>
                    <Container>
                        <Row>
                            <Col>
                                <Form.Label htmlFor="folder">Folder</Form.Label>
                                <InputGroup id="folder" className="mb-3">
                                    <InputGroup.Text className={cx((folder.startsWith("/Network/") || !folder) ? "" : "text-decoration-line-through")} >{"/Network/"}</InputGroup.Text>
                                    <Form.Control aria-label="Folder" aria-describedby="folder" value={folder.replace(/\/Network\//, "")} onChange={onFolderChange} disabled={readOnly} />
                                </InputGroup>
                            </Col>
                        </Row>
                        <Row>
                            <Col>
                                <Form.Label htmlFor="server">Server</Form.Label>
                                <InputGroup id="server" className="mb-3">
                                    <InputGroup.Text className={cx((device.startsWith("//") || !device) ? "" : "text-decoration-line-through")}>{"//"}</InputGroup.Text>
                                    <Form.Control aria-label="Server" aria-describedby="server" value={device.replace(/\/\//, "")} onChange={onServerChange} disabled={readOnly} />
                                </InputGroup>
                            </Col>
                            <Col>
                                <Form.Label id="protocolVersionLabel" htmlFor="protocolVersion">Protocol Version</Form.Label>
                                <Form.Select id="protocolVersion" aria-label="Protocol Version" aria-describedby="protocolVersionLabel" value={fields.vers} onChange={onVersionChange} disabled={readOnly} >
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
                                <Form.Control id="username" value={fields.username} onChange={onUserNameChange} disabled={readOnly} />

                            </Col>
                            <Col>
                                <Form.Label id="passwordLabel" htmlFor="password">Password</Form.Label>
                                <Form.Control type="password" id="password" aria-describedby="passwordLabel" value={fields.password} onChange={onPasswordChange} disabled={readOnly} />
                            </Col>
                        </Row>
                    </Container>
                </Tab>
                <Tab eventKey="advanced" title="Advanced" className="mb-2" disabled={readOnly}>
                    <Container>
                        <Row>
                            <Col>
                                <Form.Label id="folderLabel" htmlFor="folder">Folder</Form.Label>
                                <InputGroup className="mb-3">
                                    <Form.Control aria-label="Folder" aria-describedby="folderLabel" value={folder} onChange={onAdvancedFolderChange} disabled={readOnly} />
                                </InputGroup>
                            </Col>
                        </Row>
                        <Row>
                            <Col>
                                <Form.Label id="deviceLabel" htmlFor="folder">Device</Form.Label>
                                <InputGroup className="mb-3">
                                    <Form.Control aria-label="Device" aria-describedby="deviceLabel" value={device} onChange={onDeviceChange} disabled={readOnly} />
                                </InputGroup>
                            </Col>
                        </Row>
                        <Row>
                            <Col>
                                <Form.Label id="optionsLabel" htmlFor="folder">Options</Form.Label>
                                <InputGroup className="">
                                    <Form.Control type={!showOptions ? "password" : undefined} aria-label="Options" aria-describedby="optionsLabel" value={options} onChange={onOptionsChange} disabled={readOnly} />
                                    <Button variant="outline-secondary" onClick={() => setShowOptions(!showOptions)}><i className={cx("bi", showOptions ? "bi-eye-slash" : "bi-eye")}></i></Button>
                                </InputGroup>
                            </Col>
                        </Row>
                    </Container>
                </Tab>
            </Tabs>

        );
    }

    const NFS = ({ mount, state, setState }) => {

        const folder = state.Folder;
        const device = state.Device;
        const options = state.Options;
        // const fields = {};

        /*
        options?.split(',').forEach(pair => {
            const [key, value] = pair.split('=');
        });
        */

        const [showOptions, setShowOptions] = useState(false);

        function onFolderChange(event) {
            const value = event.target.value;
            setState({
                ...state,
                Folder: value ? `/Network/${value}` : ""
            });
        }

        function onAdvancedFolderChange(event) {
            const value = event.target.value;
            setState({
                ...state,
                Folder: value
            });
        }

        function onServerChange(event) {
            const value = event.target.value;
            const components = device.match(/^(.*):(.*)$/);
            const path = components?.[2] ?? "";
            setState({
                ...state,
                Device: (value || path) ? `${value}:${path}` : ""
            });
        }

        function onPathChange(event) {
            const value = event.target.value;
            const components = device.match(/^(.*):(.*)$/);
            const server = components?.[1] ?? "";
            setState({
                ...state,
                Device: (server || value) ? `${server}:${value}` : ""
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

        const components = device.match(/^([^:\s]+)?:?(.*)?$/);
        const server = components?.[1] ?? "";
        const path = components?.[2] ?? "";

        return (
            <Tabs
                defaultActiveKey="basic"
                id="type"
                className="mb-3"
                variant="pills"
                justify
            >
                <Tab eventKey="basic" title="Basic" className="mb-2" disabled={readOnly}>
                    <Container>
                        <Row>
                            <Col>
                                <Form.Label htmlFor="folder">Folder</Form.Label>
                                <InputGroup id="folder" className="mb-3">
                                    <InputGroup.Text className={cx((folder.startsWith("/Network/") || !folder) ? "" : "text-decoration-line-through")} >{"/Network/"}</InputGroup.Text>
                                    <Form.Control aria-label="Folder" aria-describedby="folder" value={folder.replace(/\/Network\//, "")} onChange={onFolderChange} disabled={readOnly} />
                                </InputGroup>
                            </Col>
                        </Row>
                        <Row>
                            <Col>
                                <Form.Label htmlFor="share">Share</Form.Label>
                                <InputGroup id="share" className="mb-3">
                                    <Form.Control aria-label="Server" placeholder="Server" aria-describedby="server" value={server} onChange={onServerChange} disabled={readOnly} />
                                    <InputGroup.Text>{":"}</InputGroup.Text>
                                    <Form.Control aria-label="Path" placeholder="Path" aria-describedby="server" value={path} onChange={onPathChange} disabled={readOnly} />
                                </InputGroup>
                            </Col>
                        </Row>
                    </Container>
                </Tab>
                <Tab eventKey="advanced" title="Advanced" className="mb-2" disabled={readOnly}>
                    <Container>
                        <Row>
                            <Col>
                                <Form.Label id="folderLabel" htmlFor="folder">Folder</Form.Label>
                                <InputGroup className="mb-3">
                                    <Form.Control aria-label="Folder" aria-describedby="folderLabel" value={folder} onChange={onAdvancedFolderChange} disabled={readOnly} />
                                </InputGroup>
                            </Col>
                        </Row>
                        <Row>
                            <Col>
                                <Form.Label id="deviceLabel" htmlFor="folder">Device</Form.Label>
                                <InputGroup className="mb-3">
                                    <Form.Control aria-label="Device" aria-describedby="deviceLabel" value={device} onChange={onDeviceChange} disabled={readOnly} />
                                </InputGroup>
                            </Col>
                        </Row>
                        <Row>
                            <Col>
                                <Form.Label id="optionsLabel" htmlFor="folder">Options</Form.Label>
                                <InputGroup className="">
                                    <Form.Control type={!showOptions ? "password" : undefined} aria-label="Options" aria-describedby="optionsLabel" value={options} onChange={onOptionsChange} disabled={readOnly} />
                                    <Button variant="outline-secondary" onClick={() => setShowOptions(!showOptions)}><i className={cx("bi", showOptions ? "bi-eye-slash" : "bi-eye")}></i></Button>
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
                    <Dropdown className="me-2" align="end">
                        <Dropdown.Toggle size="sm" variant="info" disabled={readOnly}>
                            <i className="bi bi-plus-lg" />
                        </Dropdown.Toggle>
                        <Dropdown.Menu>
                            <Dropdown.Item onClick={() => add("nfs")}>NFS</Dropdown.Item>
                            <Dropdown.Item onClick={() => add("cifs")}>CIFS/SMB</Dropdown.Item>
                            <Dropdown.Item onClick={() => add("sshfs")} disabled>SSHFS</Dropdown.Item>
                        </Dropdown.Menu>
                    </Dropdown>
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
                                    <Mount key={index} index={index} mount={mount} mounts={mounts} title={title} available={available} error={error}>
                                        <CIFS />
                                    </Mount>
                                ); else if (type === "nfs") return (
                                    <Mount key={index} index={index} mount={mount} mounts={mounts} title={title} available={available} error={error}>
                                        <NFS />
                                    </Mount>
                                ); else if (type === "sshfs") return (
                                    <Mount key={index} index={index} mount={mount} mounts={mounts} title={title} available={available} error={error}>
                                    </Mount>
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
