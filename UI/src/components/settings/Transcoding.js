import Form from 'react-bootstrap/Form';
import Card from 'react-bootstrap/Card';
import Badge from 'react-bootstrap/Badge';
import Button from 'react-bootstrap/Button';
import React, { useState, useEffect } from 'react';
import { Container, Row, Col } from 'react-bootstrap';
import ToggleButton from 'react-bootstrap/ToggleButton';
import ToggleButtonGroup from 'react-bootstrap/ToggleButtonGroup';

export default function Transcoding({ settings, write }) {

    const [hardwareAcceleration, setHardwareAcceleration] = useState(null);
    const [videoEncoder, setVideoEncoder] = useState(null);
    const [videoDecoder, setVideoDecoder] = useState(null);
    const [audioEncoder, setAudioEncoder] = useState(null);
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
            <Row>
                <Col className="my-1 mb-3">
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
                                            <Form.Select size="sm" value={hardwareAcceleration ?? ""} onChange={(event) => onChange('HardwareAcceleration', event.currentTarget.value)}>
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
                                            <Form.Select size="sm" value={videoEncoder ?? ""} onChange={(event) => onChange('VideoEncoder', event.currentTarget.value)}>
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
                                            <Form.Select size="sm" value={audioEncoder ?? ""} onChange={(event) => onChange('AudioEncoder', event.currentTarget.value)}>
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
