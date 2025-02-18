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
import { requeueFailedUpload, removeUploads } from '../../features/ui/slice';
import React, { useState, useEffect, forwardRef, useImperativeHandle } from 'react';

const Uploads = forwardRef((props, ref) => {
    const dispatch = useDispatch();

    const uploads = useSelector((state) => state.ui.uploads);

    const [state, setState] = useState(false);
    const [tab, setTab] = useState('all');

    useImperativeHandle(ref, () => ({
        show() {
            setState(true);
        }
    }));

    function onShow() {
        props.onShow?.()
    }

    function onHide() {
        setState(false);
        props.onHide?.()
    }

    function onRemove(type, key) {
        dispatch(removeUploads({type: type, key: key}));
    }

    function onRetry(key) {
        dispatch(requeueFailedUpload({key: key}));
        props.onUpload?.()
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
        switch (upload.type) {
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
                                        <strong>{upload.name ?? upload.url}</strong>
                                    </Col>
                                    <Col className="gx-0 small text-muted" xs={12}>
                                    {
                                        (upload.url) ?
                                            <a href="#" className="text-decoration-none" onClick={() => onOpen(null, null, upload.url)}>{upload.url ?? upload.path}</a>
                                            : upload.url ?? upload.path
                                    }
                                    </Col>
                                </Row>
                                {
                                    (upload.progress != null) ?
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
                                (upload.url && upload.type === 'failed') ?
                                    <Button variant="outline-info" size="sm" onClick={() => onRetry(upload.key)}>Retry</Button>
                                    : <></>
                            }
                            {
                                (upload.type === 'queued' || upload.type === 'succeeded' || upload.type === 'failed') ?
                                    <Button variant="outline-danger ms-2" size="sm" onClick={() => onRemove(upload.type, upload.key)}>Remove</Button>
                                    : <></>
                            }
                        </Col>
                    </Row>
                </Container>
            </ListGroup.Item>
        )
    }

    const UploadListGroup = ({ source, types }) => {
        if (source.length === 0) return <></>;
        else return (
            <>
                <ListGroup variant="flush">
                {
                    source.map((item, index) => {
                        return (
                            <Upload key={index} upload={item} />
                        )
                    })
                }
                </ListGroup>
                <Container fluid>
                    <Row className="pt-1">
                        <Col className="d-flex align-items-center">
                            <Button variant="danger" className="d-flex flex-grow-1 justify-content-center" onClick={() => {
                                if (types) {
                                    types.forEach(type => {
                                        dispatch(removeUploads({type: type}))
                                    });
                                }
                        }}>Clear</Button>
                        </Col>
                    </Row>
                </Container>
            </>
        )
    }

    const active = [
        ...Object.keys(uploads.active).map((key) => {
            return {
                type: 'active',
                ...uploads.active[key]
            }
        })
    ].sort((a, b) => b.timestamp - a.timestamp);

    const queued = [
        ...uploads.queued.map((item) => {
            return {
                type: 'queued',
                ...item
            }
        })
    ].sort((a, b) => b.timestamp - a.timestamp);

    const succeeded = [
        ...uploads.succeeded.map((item) => {
            return {
                type: 'succeeded',
                ...item
            }
        })
    ].sort((a, b) => b.timestamp - a.timestamp);

    const failed = [
        ...uploads.failed.map((item) => {
            return {
                type: 'failed',
                ...item
            }
        })
    ].sort((a, b) => b.timestamp - a.timestamp);

    const all = [
        ...active, ...queued, ...succeeded, ...failed
    ].sort((a, b) => b.timestamp - a.timestamp);

    return (
        <Modal className="uploads" show={state} onShow={onShow} onHide={onHide} backdrop={true} animation={true} size={"lg"} aria-labelledby="contained-modal-title-vcenter" centered>
            <Modal.Header className="flex-row align-items-center me-3" closeButton>
                <Modal.Title className="ms-2" id="contained-modal-title-vcenter">
                    Uploads
                </Modal.Title>
            </Modal.Header>
            <Modal.Body>
                <Tabs id="uploads-tabs" className="flex-row mb-3" activeKey={tab} onSelect={(tab) => setTab(tab)} variant="tabs" navbar justify>
                    <Tab id="tab-all" eventKey="all" title="All">
                        <UploadListGroup types={["active", "queued", "succeeded", "failed"]} source={all}/>
                    </Tab>
                    <Tab id="tab-queued" eventKey="queued" title={<><i className="bi bi-circle text-secondary pe-2"/>Queued</>}>
                        <UploadListGroup types={["queued"]} source={queued}/>
                    </Tab>
                    <Tab id="tab-active" eventKey="active" title={<><i className="bi bi-circle-fill text-info pe-2"/>Active</>}>
                        <UploadListGroup types={["active"]} source={active}/>
                    </Tab>
                    <Tab id="tab-succeeded" eventKey="succeeded" title={<><i className="bi bi-check-circle text-success pe-2"/>Succeeded</>}>
                        <UploadListGroup types={["succeeded"]} source={succeeded}/>
                    </Tab>
                    <Tab id="tab-failed" eventKey={["failed"]} title={<><i className="bi bi-check-circle text-danger pe-2"/>Failed</>}>
                        <UploadListGroup types="failed" source={failed}/>
                    </Tab>
                </Tabs>
            </Modal.Body>
        </Modal>
    );
});

export default Uploads;
