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
import pb from 'path-browserify';
import Tab from 'react-bootstrap/Tab';
import Tabs from 'react-bootstrap/Tabs';
import Modal from 'react-bootstrap/Modal';
import Button from 'react-bootstrap/Button';
import ListGroup from 'react-bootstrap/ListGroup';
import ProgressBar from 'react-bootstrap/ProgressBar';
import { Container, Row, Col } from 'react-bootstrap';
import { useDispatch, useSelector } from 'react-redux';
import React, { useState, forwardRef, useImperativeHandle } from 'react';
import { switchUploadStateThunk, removeUploads } from '../../features/ui/slice';
import { selectAll, selectActive, selectQueued, selectSucceeded, selectFailed } from '../../features/ui/selectors';

const Uploads = forwardRef((props, ref) => {
    const dispatch = useDispatch();

    const [state, setState] = useState(false);
    const [tab, setTab] = useState('all');

    const all = useSelector(selectAll);
    const active = useSelector(selectActive);
    const queued = useSelector(selectQueued);
    const failed = useSelector(selectFailed);
    const succeeded = useSelector(selectSucceeded);

    useImperativeHandle(ref, () => ({
        show() {
            setState(true);
        },
        get open() {
            return state;
        },
    }));

    function onShow() {
        props.onShow?.();
    }

    function onHide() {
        setState(false);
        props.onHide?.();
    }

    function onRetry(key) {
        dispatch(switchUploadStateThunk(key, 'queued')).then(() => {
            props.onUpload?.();
        });
    }

    function onOpen(path, name, url) {
        if (path && name) {
            props.onOpen?.(pb.join(path, name));
        } else if (path) {
            props.onOpen?.(path);
        } else if (url) {
            props.onOpen?.(url);
        }
    }

    const Upload = ({ upload }) => {
        let icon = null;
        let color = null;
        switch (upload.state) {
            case 'queued':
                color = 'text-secondary';
                icon = 'bi bi-circle';
                break;
            case 'active':
                color = 'text-info';
                icon = 'bi bi-circle-fill';
                break;
            case 'succeeded':
                color = 'text-success';
                icon = 'bi-check-circle';
                break;
            case 'failed':
                color = 'text-danger';
                icon = 'bi-x-circle';
                break;

            default:
                break;
        }

        return (
            <ListGroup.Item key={upload.key}>
                <Container fluid>
                    <Row>
                        <Col className={cx("d-flex h4 mb-0 gx-0 pe-3 align-items-center", color)} xs="auto">
                            <i className={cx("bi", icon)} />
                        </Col>
                        <Col className="d-flex align-items-center gx-0">
                            <Container fluid>
                                <Row>
                                    <Col className="gx-0 pb-1" xs={12}>
                                    {
                                        (upload.state === 'succeeded') ?
                                            <a href={pb.join(upload.path, upload.name)} className="text-decoration-none text-body" onClick={() => onOpen(upload.path, upload.name)}><strong>{upload.name ?? upload.url}</strong></a> :
                                            <strong>{upload.name ?? upload.url}</strong>
                                    }
                                    </Col>
                                    <Col className="gx-0 small text-muted" xs={12}>
                                    {
                                        (upload.url) ?
                                            <a href={upload.url} className="text-decoration-none" onClick={() => onOpen(null, null, upload.url)}>{upload.url}</a>
                                            : <></>
                                    }
                                    {
                                        (!upload.url && upload.path) ? upload.path : <></>
                                    }
                                    </Col>
                                </Row>
                                {
                                    (upload.state === 'active' && upload.progress != null) ?
                                        <Row>
                                            <Col className="pt-2 gx-0" xs={12}>
                                                <ProgressBar variant="info" min={0.0} now={upload.progress} max={1.0} animated={false} />
                                            </Col>
                                        </Row>
                                        : <></>
                                }
                            </Container>
                        </Col>
                        <Col className="d-flex align-items-center gx-0" xs="auto">
                        {
                            (upload.url && upload.state === 'failed') ?
                                <Button variant="outline-info" size="sm" onClick={() => onRetry(upload.key)}>Retry</Button>
                                : <></>
                        }
                        {
                            (upload.state === 'queued' || upload.state === 'succeeded' || upload.state === 'failed') ?
                                <Button variant="outline-danger ms-2" size="sm" onClick={() => dispatch(removeUploads({ key: upload.key }))}>Remove</Button>
                                : <></>
                        }
                        </Col>
                    </Row>
                </Container>
            </ListGroup.Item>
        )
    }

    const UploadListGroup = ({ uploads, clear, retry }) => {
        if (uploads.length === 0) return <></>;
        else return (
            <>
                <ListGroup className="flex-grow-1" variant="flush">
                {
                    uploads.map((item, index) => {
                        return (
                            <Upload key={index} upload={item} />
                        )
                    })
                }
                </ListGroup>
                <Container fluid>
                    <Row className="pt-1">
                    {
                        (retry) ?
                        <Col className="d-flex align-items-center">
                            <Button variant="info" className="d-flex flex-grow-1 justify-content-center" onClick={() => {
                                uploads.forEach(upload => {
                                    dispatch(switchUploadStateThunk(upload.key, 'queued')).then(() => {
                                        props.onUpload?.();
                                    });
                                });
                            }}>Retry</Button>
                        </Col> : <></>
                    }
                    {
                        (clear) ?
                        <Col className="d-flex align-items-center">
                            <Button variant="danger" className="d-flex flex-grow-1 justify-content-center" onClick={() => {
                                clear.forEach(state => {
                                    dispatch(removeUploads({ state: state }))
                                });
                            }}>Clear</Button>
                        </Col> : <></>
                    }
                    </Row>
                </Container>
            </>
        )
    }

    return (
        <Modal className="uploads" show={state} onShow={onShow} onHide={onHide} backdrop={true} animation={true} size={"lg"} aria-labelledby="contained-modal-title-vcenter" centered>
            <Modal.Header className="flex-row align-items-center me-3" closeButton>
                <Modal.Title className="ms-2" id="contained-modal-title-vcenter">
                    Uploads
                </Modal.Title>
            </Modal.Header>
            <Modal.Body className="d-flex flex-column p-0">
                <Tabs id="uploads-tabs" className="flex-row mb-2 px-2 pt-2" activeKey={tab} onSelect={(tab) => setTab(tab)} variant="tabs" navbar justify>
                    <Tab id="tab-all" className="flex-column flex-grow-1" eventKey="all" title={
                        <>
                            <p className="mb-1">All</p>
                            <span>{`(${all.length})`}</span>
                        </>
                    }>
                        <UploadListGroup clear={["queued", "succeeded", "failed"]} uploads={all}/>
                    </Tab>
                    <Tab id="tab-queued" className="flex-column flex-grow-1" eventKey="queued" title={
                        <>
                            <p className="mb-1"><i className="bi bi-circle text-secondary pe-2"/>Queued</p>
                            <span>{`(${queued.length})`}</span>
                        </>
                    }>
                        <UploadListGroup clear={["queued"]} uploads={queued}/>
                    </Tab>
                    <Tab id="tab-active" className="flex-column flex-grow-1" eventKey="active" title={
                        <>
                            <p className="mb-1"><i className="bi bi-circle-fill text-info pe-2"/>Active</p>
                            <span>{`(${active.length})`}</span>
                        </>
                    }>
                        <UploadListGroup uploads={active}/>
                    </Tab>
                    <Tab id="tab-succeeded" className="flex-column flex-grow-1" eventKey="succeeded" title={
                        <>
                            <p className="mb-1"><i className="bi bi-check-circle text-success pe-2"/>Succeeded</p>
                            <span>{`(${succeeded.length})`}</span>
                        </>
                    }>
                        <UploadListGroup clear={["succeeded"]} uploads={succeeded}/>
                    </Tab>
                    <Tab id="tab-failed" className="flex-column flex-grow-1" eventKey="failed" title={
                        <>
                            <p className="mb-1"><i className="bi bi-check-circle text-danger pe-2"/>Failed</p>
                            <span>{`(${failed.length})`}</span>
                        </>
                    }>
                        <UploadListGroup clear={["failed"]} retry={true} uploads={failed}/>
                    </Tab>
                </Tabs>
            </Modal.Body>
        </Modal>
    );
});

export default Uploads;
