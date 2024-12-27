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

import React from 'react';
import { shorten } from '../utils';
import Row from 'react-bootstrap/Row';
import Col from 'react-bootstrap/Col';
import Container from 'react-bootstrap/Container';

const ProgressToast = ({ className, title, subtitle, ...props }) => {
    return (
        <Container className="progress-toast gx-1">
            <Row>
                <Col className="progress-toast-title">
                    <strong>{title}</strong>
                </Col>
            </Row>
            <Row className="">
                <Col className="progress-toast-subtitle">
                    {
                        subtitle ? <small>{shorten(subtitle, 100)}</small> : <></>
                    }
                </Col>
            </Row>
        </Container>
    );
};

export default ProgressToast;