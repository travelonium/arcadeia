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

import Modal from 'react-bootstrap/Modal';
import Button from 'react-bootstrap/Button';
import { Container, Row, Col } from 'react-bootstrap';
import { useState, forwardRef, useImperativeHandle } from 'react';

const Selection = forwardRef((props, ref) => {
    const [state, setState] = useState(false);

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

    function select(predicate, label) {
        props.onSelect?.(predicate);
        onHide();
    }

    return (
        <Modal className="selection" show={state} onShow={onShow} onHide={onHide} backdrop={true} animation={true} size={"md"} aria-labelledby="contained-modal-title-vcenter" centered>
            <Modal.Header className="flex-row align-items-center me-3" closeButton>
                <Modal.Title className="ms-2" id="contained-modal-title-vcenter">
                    Selection
                </Modal.Title>
            </Modal.Header>
            <Modal.Body className="d-flex flex-column p-4">
                <Container fluid>
                    <Row className="mb-3">
                        <Col>
                            <Button
                                variant="outline-secondary"
                                className="w-100 d-flex align-items-center justify-content-center"
                                size="lg"
                                onClick={() => select(item => true, 'All')}
                            >
                                <i className="bi bi-check-square me-2" />
                                All
                            </Button>
                        </Col>
                    </Row>
                    <Row className="mb-3">
                        <Col>
                            <Button
                                variant="outline-secondary"
                                className="w-100 d-flex align-items-center justify-content-center"
                                size="lg"
                                onClick={() => select(item => false, 'None')}
                            >
                                <i className="bi bi-square me-2" />
                                None
                            </Button>
                        </Col>
                    </Row>
                    <Row className="mb-3">
                        <Col>
                            <Button
                                variant="outline-secondary"
                                className="w-100 d-flex align-items-center justify-content-center"
                                size="lg"
                                onClick={() => select(item => item.type === 'Video', 'Videos')}
                            >
                                <i className="bi bi-film me-2" />
                                Videos
                            </Button>
                        </Col>
                    </Row>
                    <Row className="mb-3">
                        <Col>
                            <Button
                                variant="outline-secondary"
                                className="w-100 d-flex align-items-center justify-content-center"
                                size="lg"
                                onClick={() => select(item => item.type === 'Photo', 'Photos')}
                            >
                                <i className="bi bi-image me-2" />
                                Photos
                            </Button>
                        </Col>
                    </Row>
                    <Row className="mb-3">
                        <Col>
                            <Button
                                variant="outline-secondary"
                                className="w-100 d-flex align-items-center justify-content-center"
                                size="lg"
                                onClick={() => select('duplicates', 'Duplicates')}
                            >
                                <i className="bi bi-files me-2" />
                                Duplicates
                            </Button>
                        </Col>
                    </Row>
                </Container>
            </Modal.Body>
        </Modal>
    );
});

Selection.displayName = "Selection";

export default Selection;
