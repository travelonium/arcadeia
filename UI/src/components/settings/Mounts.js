import { Container, Row, Col } from 'react-bootstrap';
// import React, { useState, useEffect, useCallback } from 'react';

export default function Mounts({ settings, write }) {

    return (
        <Container>
            <Row>
                <Col className="my-1 mb-3">
                    <h2>Network Mounts</h2>
                </Col>
            </Row>
        </Container>
    );
};
