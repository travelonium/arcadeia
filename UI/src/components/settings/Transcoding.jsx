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

import Form from 'react-bootstrap/Form';
import Card from 'react-bootstrap/Card';
import React, { useState, useEffect } from 'react';
import { Container, Row, Col } from 'react-bootstrap';

export default function Transcoding({ settings, write, readOnly }) {

    const [hardwareAcceleration, setHardwareAcceleration] = useState(null);
    const [videoEncoder, setVideoEncoder] = useState(null);
    // eslint-disable-next-line
    const [videoDecoder, setVideoDecoder] = useState(null);
    const [audioEncoder, setAudioEncoder] = useState(null);
    // eslint-disable-next-line
    const [audioDecoder, setAudioDecoder] = useState(null);

    useEffect(() => {
        setHardwareAcceleration(settings?.FFmpeg?.HardwareAcceleration);
        setVideoEncoder(settings?.FFmpeg?.Encoder?.Video);
        setVideoDecoder(settings?.FFmpeg?.Decoder?.Video);
        setAudioEncoder(settings?.FFmpeg?.Encoder?.Audio);
        setAudioDecoder(settings?.FFmpeg?.Decoder?.Audio);
    }, [settings]);

    function onChange(setting, value) {
        switch (setting) {
            case 'HardwareAcceleration':
                setHardwareAcceleration(value);
                write({ FFmpeg: { HardwareAcceleration: value ? value : null } });
                break;

            case 'VideoEncoder':
                setVideoEncoder(value);
                write({ FFmpeg: { Encoder: { Video: value ? value : null }} });
                break;

            case 'VideoDecoder':
                setVideoDecoder(value);
                write({ FFmpeg: { Decoder: { Video: value ? value : null }} });
                break;

            case 'AudioEncoder':
                setAudioEncoder(value);
                write({ FFmpeg: { Encoder: { Audio: value ? value : null }} });
                break;

            case 'AudioDecoder':
                setAudioDecoder(value);
                write({ FFmpeg: { Decoder: { Audio: value ? value : null }} });
                break;

            default:
                break;
        }
    }

    return (
        <Container>
            <Row className="align-items-center mb-2">
                <Col className="my-1">
                    <h2>Transcoding</h2>
                </Col>
            </Row>
            <Row>
                <Col>
                    <Container>

                        <Row className="startup-scan align-items-center mb-3">
                            <Card className="px-0">
                                <Card.Header className="pe-2">
                                    <Row className="align-items-center">
                                        <Col><b>Hardware Acceleration</b></Col>
                                        <Col xs="auto">
                                            <Form.Select size="sm" value={hardwareAcceleration ?? ""} onChange={(event) => onChange('HardwareAcceleration', event.currentTarget.value)} disabled={readOnly}>
                                                <option value="">Disabled</option>
                                            {
                                                settings?.System?.FFmpeg?.HardwareAcceleration?.map((method) => <option key={method} value={method}>{method}</option>)
                                            }
                                            </Form.Select>
                                        </Col>
                                    </Row>
                                </Card.Header>
                                <Card.Body>
                                    <Card.Text className="d-flex align-items-center">
                                        <i className="bi bi-question-circle text-info pe-2"></i>
                                        Select an available hardware acceleration method or disable hardware acceleration.
                                    </Card.Text>
                                </Card.Body>
                            </Card>
                        </Row>

                        <Row className="startup-scan align-items-center mb-3">
                            <Card className="px-0">
                                <Card.Header className="pe-2">
                                    <Row className="align-items-center">
                                        <Col><b>Video Encoder</b></Col>
                                        <Col xs="auto">
                                            <Form.Select size="sm" value={videoEncoder ?? ""} onChange={(event) => onChange('VideoEncoder', event.currentTarget.value)} disabled={readOnly}>
                                                <option value="">Default</option>
                                            {
                                                settings?.System?.FFmpeg?.Codecs?.Encoders?.Video?.map((encoder) => <option key={encoder.Name} value={encoder.Name} title={encoder.Description}>{encoder.Name}</option>)
                                            }
                                            </Form.Select>
                                        </Col>
                                    </Row>
                                </Card.Header>
                                <Card.Body>
                                    <Card.Text className="d-flex align-items-center">
                                        <i className="bi bi-question-circle text-info pe-2"></i>
                                        Select an available hardware acceleration method or disable hardware acceleration.
                                    </Card.Text>
                                </Card.Body>
                            </Card>
                        </Row>

                        <Row className="startup-scan align-items-center mb-3">
                            <Card className="px-0">
                                <Card.Header className="pe-2">
                                    <Row className="align-items-center">
                                        <Col><b>Audio Encoder</b></Col>
                                        <Col xs="auto">
                                            <Form.Select size="sm" value={audioEncoder ?? ""} onChange={(event) => onChange('AudioEncoder', event.currentTarget.value)} disabled={readOnly}>
                                                <option value="">Default</option>
                                            {
                                                settings?.System?.FFmpeg?.Codecs?.Encoders?.Audio?.map((encoder) => <option key={encoder.Name} value={encoder.Name} title={encoder.Description}>{encoder.Name}</option>)
                                            }
                                            </Form.Select>
                                        </Col>
                                    </Row>
                                </Card.Header>
                                <Card.Body>
                                    <Card.Text className="d-flex align-items-center">
                                        <i className="bi bi-question-circle text-info pe-2"></i>
                                        Select an available hardware acceleration method or disable hardware acceleration.
                                    </Card.Text>
                                </Card.Body>
                            </Card>
                        </Row>

                    </Container>
                </Col>
            </Row>
        </Container>
    );
};
