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
import Badge from 'react-bootstrap/Badge';
import { useState, useEffect } from 'react';
import { Container, Row, Col } from 'react-bootstrap';
import { useDispatch, useSelector } from 'react-redux';
import ToggleButton from 'react-bootstrap/ToggleButton';
import { writeSettings } from '../../features/settings/slice';
import ToggleButtonGroup from 'react-bootstrap/ToggleButtonGroup';

// The ggml model sizes whisper.cpp publishes for general (non ".en") multilingual use. Not
// downloaded ahead of time - only the default is bundled, any other model configured here is
// fetched automatically the first time it's needed (see VideoFile.EnsureModelDownloaded).
const MODELS = [
    { name: 'tiny', label: 'Tiny' },
    { name: 'base', label: 'Base' },
    { name: 'small', label: 'Small' },
    { name: 'medium', label: 'Medium' },
    { name: 'large-v3', label: 'Large' },
];

// The languages whisper.cpp can transcribe (from its language table), sorted alphabetically.
const LANGUAGES = [
    { code: 'en', name: 'english' }, { code: 'zh', name: 'chinese' }, { code: 'de', name: 'german' },
    { code: 'es', name: 'spanish' }, { code: 'ru', name: 'russian' }, { code: 'ko', name: 'korean' },
    { code: 'fr', name: 'french' }, { code: 'ja', name: 'japanese' }, { code: 'pt', name: 'portuguese' },
    { code: 'tr', name: 'turkish' }, { code: 'pl', name: 'polish' }, { code: 'ca', name: 'catalan' },
    { code: 'nl', name: 'dutch' }, { code: 'ar', name: 'arabic' }, { code: 'sv', name: 'swedish' },
    { code: 'it', name: 'italian' }, { code: 'id', name: 'indonesian' }, { code: 'hi', name: 'hindi' },
    { code: 'fi', name: 'finnish' }, { code: 'vi', name: 'vietnamese' }, { code: 'he', name: 'hebrew' },
    { code: 'uk', name: 'ukrainian' }, { code: 'el', name: 'greek' }, { code: 'ms', name: 'malay' },
    { code: 'cs', name: 'czech' }, { code: 'ro', name: 'romanian' }, { code: 'da', name: 'danish' },
    { code: 'hu', name: 'hungarian' }, { code: 'ta', name: 'tamil' }, { code: 'no', name: 'norwegian' },
    { code: 'th', name: 'thai' }, { code: 'ur', name: 'urdu' }, { code: 'hr', name: 'croatian' },
    { code: 'bg', name: 'bulgarian' }, { code: 'lt', name: 'lithuanian' }, { code: 'la', name: 'latin' },
    { code: 'mi', name: 'maori' }, { code: 'ml', name: 'malayalam' }, { code: 'cy', name: 'welsh' },
    { code: 'sk', name: 'slovak' }, { code: 'te', name: 'telugu' }, { code: 'fa', name: 'persian' },
    { code: 'lv', name: 'latvian' }, { code: 'bn', name: 'bengali' }, { code: 'sr', name: 'serbian' },
    { code: 'az', name: 'azerbaijani' }, { code: 'sl', name: 'slovenian' }, { code: 'kn', name: 'kannada' },
    { code: 'et', name: 'estonian' }, { code: 'mk', name: 'macedonian' }, { code: 'br', name: 'breton' },
    { code: 'eu', name: 'basque' }, { code: 'is', name: 'icelandic' }, { code: 'hy', name: 'armenian' },
    { code: 'ne', name: 'nepali' }, { code: 'mn', name: 'mongolian' }, { code: 'bs', name: 'bosnian' },
    { code: 'kk', name: 'kazakh' }, { code: 'sq', name: 'albanian' }, { code: 'sw', name: 'swahili' },
    { code: 'gl', name: 'galician' }, { code: 'mr', name: 'marathi' }, { code: 'pa', name: 'punjabi' },
    { code: 'si', name: 'sinhala' }, { code: 'km', name: 'khmer' }, { code: 'sn', name: 'shona' },
    { code: 'yo', name: 'yoruba' }, { code: 'so', name: 'somali' }, { code: 'af', name: 'afrikaans' },
    { code: 'oc', name: 'occitan' }, { code: 'ka', name: 'georgian' }, { code: 'be', name: 'belarusian' },
    { code: 'tg', name: 'tajik' }, { code: 'sd', name: 'sindhi' }, { code: 'gu', name: 'gujarati' },
    { code: 'am', name: 'amharic' }, { code: 'yi', name: 'yiddish' }, { code: 'lo', name: 'lao' },
    { code: 'uz', name: 'uzbek' }, { code: 'fo', name: 'faroese' }, { code: 'ht', name: 'haitian creole' },
    { code: 'ps', name: 'pashto' }, { code: 'tk', name: 'turkmen' }, { code: 'nn', name: 'nynorsk' },
    { code: 'mt', name: 'maltese' }, { code: 'sa', name: 'sanskrit' }, { code: 'lb', name: 'luxembourgish' },
    { code: 'my', name: 'myanmar' }, { code: 'bo', name: 'tibetan' }, { code: 'tl', name: 'tagalog' },
    { code: 'mg', name: 'malagasy' }, { code: 'as', name: 'assamese' }, { code: 'tt', name: 'tatar' },
    { code: 'haw', name: 'hawaiian' }, { code: 'ln', name: 'lingala' }, { code: 'ha', name: 'hausa' },
    { code: 'ba', name: 'bashkir' }, { code: 'jw', name: 'javanese' }, { code: 'su', name: 'sundanese' },
    { code: 'yue', name: 'cantonese' },
].sort((a, b) => a.name.localeCompare(b.name));

export default function Transcription() {
    const dispatch = useDispatch();

    const settings = useSelector((state) => state.settings.current);
    const readOnly = useSelector((state) => state.settings.current?.Security?.Settings?.ReadOnly);

    const [enabled, setEnabled] = useState(null);
    const [model, setModel] = useState('');
    const [language, setLanguage] = useState('auto');
    const [timeoutMinutes, setTimeoutMinutes] = useState('');
    const [parallelTasks, setParallelTasks] = useState(null);
    const [catchUpOnStartup, setCatchUpOnStartup] = useState(null);

    useEffect(() => {
        setEnabled(settings?.Transcription?.Enabled);
        setModel(settings?.Transcription?.Model ?? '');
        setLanguage(settings?.Transcription?.Language ?? 'auto');
        setTimeoutMinutes(settings?.Transcription?.TimeoutMilliseconds != null ? Math.round(settings.Transcription.TimeoutMilliseconds / (60 * 1000)) : '');
        setParallelTasks(settings?.Transcription?.ParallelTasks);
        setCatchUpOnStartup(settings?.Transcription?.CatchUpOnStartup);
    }, [settings]);

    // The directory the currently configured model lives in, so switching models doesn't change
    // where they're expected to be found (e.g. "/usr/share/whisper/" in the container).
    const modelDirectory = model.substring(0, model.lastIndexOf('/') + 1);

    function onChange(setting, value) {
        switch (setting) {
            case 'Enabled':
                setEnabled(value);
                dispatch(writeSettings({ Transcription: { Enabled: value } }));
                break;

            case 'Model':
                setModel(value);
                dispatch(writeSettings({ Transcription: { Model: value } }));
                break;

            case 'Language':
                setLanguage(value);
                dispatch(writeSettings({ Transcription: { Language: value } }));
                break;

            case 'TimeoutMilliseconds':
                if (!value || value < 1) break;
                setTimeoutMinutes(value);
                dispatch(writeSettings({ Transcription: { TimeoutMilliseconds: value * 60 * 1000 } }));
                break;

            case 'ParallelTasks':
                setParallelTasks(value);
                dispatch(writeSettings({ Transcription: { ParallelTasks: value } }));
                break;

            case 'CatchUpOnStartup':
                setCatchUpOnStartup(value);
                dispatch(writeSettings({ Transcription: { CatchUpOnStartup: value } }));
                break;

            default:
                console.error(`${setting} is not implemented!`);
                break;
        }
    }

    return (
        <Container>
            <Row className="align-items-center mb-2">
                <Col className="my-1">
                    <h2>Transcription</h2>
                </Col>
            </Row>
            <Row>
                <Col>
                    <Container>

                        <Row className="enabled align-items-center mb-3">
                            <Card className="px-0">
                                <Card.Header className="pe-2">
                                    <Row className="align-items-center">
                                        <Col><b>Enabled</b></Col>
                                        <Col xs="auto">
                                            <ToggleButtonGroup name="transcription-enabled" value={enabled ? 1 : 0} onChange={(value) => onChange('Enabled', value > 0)}>
                                                <ToggleButton type="radio" size="sm" id="transcription-enabled-off" variant="outline-secondary" name="radio" disabled={enabled == null || readOnly} value={0}>OFF</ToggleButton>
                                                <ToggleButton type="radio" size="sm" id="transcription-enabled-on" variant="outline-info" name="radio" disabled={enabled == null || readOnly} value={1}>ON</ToggleButton>
                                            </ToggleButtonGroup>
                                        </Col>
                                    </Row>
                                </Card.Header>
                                <Card.Body>
                                    <Card.Text className="d-flex align-items-center">
                                        <i className="bi bi-question-circle text-info pe-2"></i>
                                        Transcribe the speech in video files during scanning and index the result for search.
                                    </Card.Text>
                                </Card.Body>
                            </Card>
                        </Row>

                        <Row className="model align-items-center mb-3">
                            <Card className="px-0">
                                <Card.Header className="pe-2">
                                    <Row className="align-items-center">
                                        <Col><b>Model</b></Col>
                                        <Col xs="auto">
                                            <Form.Select size="sm" style={{width: "16rem"}} aria-label="Model" value={model} disabled={readOnly}
                                                onChange={(event) => onChange('Model', event.target.value)}>
                                            {
                                                MODELS.map((item) => {
                                                    const filename = `ggml-${item.name}.bin`;
                                                    const downloaded = settings?.System?.Transcription?.Models?.includes(filename);
                                                    return <option key={item.name} value={`${modelDirectory}${filename}`}>{item.label} {downloaded ? '(Downloaded)' : ''}</option>;
                                                })
                                            }
                                            </Form.Select>
                                        </Col>
                                    </Row>
                                </Card.Header>
                                <Card.Body>
                                    <Card.Text className="d-flex align-items-center">
                                        <i className="bi bi-question-circle text-info pe-2"></i>
                                        The Whisper model to transcribe with. Larger models are more accurate, especially for non-English speech, but slower. If not already present, it&apos;s downloaded automatically the first time it&apos;s needed.
                                    </Card.Text>
                                </Card.Body>
                            </Card>
                        </Row>

                        <Row className="language align-items-center mb-3">
                            <Card className="px-0">
                                <Card.Header className="pe-2">
                                    <Row className="align-items-center">
                                        <Col><b>Language</b></Col>
                                        <Col xs="auto">
                                            <Form.Select size="sm" style={{width: "12rem"}} aria-label="Language" value={language} disabled={readOnly}
                                                onChange={(event) => onChange('Language', event.target.value)}>
                                                <option value="auto">Detect Automatically</option>
                                            {
                                                LANGUAGES.map((item) => <option key={item.code} value={item.code}>{item.name.charAt(0).toUpperCase() + item.name.slice(1)}</option>)
                                            }
                                            </Form.Select>
                                        </Col>
                                    </Row>
                                </Card.Header>
                                <Card.Body>
                                    <Card.Text className="d-flex align-items-center">
                                        <i className="bi bi-question-circle text-info pe-2"></i>
                                        The spoken language to transcribe, or detect it automatically.
                                    </Card.Text>
                                </Card.Body>
                            </Card>
                        </Row>

                        <Row className="timeout align-items-center mb-3">
                            <Card className="px-0">
                                <Card.Header className="pe-2">
                                    <Row className="align-items-center">
                                        <Col><b>Timeout</b></Col>
                                        <Col xs="auto" className="d-flex align-items-center">
                                            <Form.Control type="number" size="sm" style={{width: "6rem"}} min={1} aria-label="Timeout" value={timeoutMinutes} disabled={readOnly}
                                                onChange={(event) => setTimeoutMinutes(event.target.value)}
                                                onBlur={(event) => onChange('TimeoutMilliseconds', parseInt(event.target.value, 10))} />
                                            <span className="ms-2">minutes</span>
                                        </Col>
                                    </Row>
                                </Card.Header>
                                <Card.Body>
                                    <Card.Text className="d-flex align-items-center">
                                        <i className="bi bi-question-circle text-info pe-2"></i>
                                        The maximum time to allow a single video&apos;s transcription to run before it&apos;s aborted.
                                    </Card.Text>
                                </Card.Body>
                            </Card>
                        </Row>

                        <Row className="parallel-tasks align-items-center mb-3">
                            <Card className="px-0">
                                <Card.Header className="pe-2">
                                    <Row className="align-items-center">
                                        <Col><b>Parallel Tasks</b></Col>
                                        <Col className="d-flex" xs="auto">
                                            <Badge bg="info">
                                                <span className="parallel-tasks-value" title="Parallel Tasks / CPU Cores">{parallelTasks ?? 0} / {settings?.System?.ProcessorCount}</span>
                                            </Badge>
                                        </Col>
                                        <Col className="d-flex mt-3 mt-sm-0" xs={12} sm="auto">
                                            <Form.Range min={1} max={settings?.System?.ProcessorCount} step={1}
                                                name="transcription-parallel-tasks"
                                                disabled={parallelTasks == null || readOnly}
                                                value={parallelTasks ?? 0}
                                                onChange={(event) => setParallelTasks(event.target.value)}
                                                onMouseUp={(event) => onChange('ParallelTasks', event.target.value)}
                                                onTouchEnd={(event) => onChange('ParallelTasks', event.target.value)}
                                                onKeyUp={(event) => onChange('ParallelTasks', event.target.value)} />
                                        </Col>
                                    </Row>
                                </Card.Header>
                                <Card.Body>
                                    <Card.Text className="d-flex align-items-center">
                                        <i className="bi bi-question-circle text-info pe-2"></i>
                                        Adjusts the number of videos transcribed in parallel. Kept low by default since transcription is CPU-intensive.
                                    </Card.Text>
                                </Card.Body>
                            </Card>
                        </Row>

                        <Row className="catch-up-on-startup align-items-center mb-3">
                            <Card className="px-0">
                                <Card.Header className="pe-2">
                                    <Row className="align-items-center">
                                        <Col><b>Catch Up On Startup</b></Col>
                                        <Col xs="auto">
                                            <ToggleButtonGroup name="catch-up-on-startup" value={catchUpOnStartup ? 1 : 0} onChange={(value) => onChange('CatchUpOnStartup', value > 0)}>
                                                <ToggleButton type="radio" size="sm" id="catch-up-on-startup-off" variant="outline-secondary" name="radio" disabled={catchUpOnStartup == null || readOnly} value={0}>OFF</ToggleButton>
                                                <ToggleButton type="radio" size="sm" id="catch-up-on-startup-on" variant="outline-info" name="radio" disabled={catchUpOnStartup == null || readOnly} value={1}>ON</ToggleButton>
                                            </ToggleButtonGroup>
                                        </Col>
                                    </Row>
                                </Card.Header>
                                <Card.Body>
                                    <Card.Text className="d-flex align-items-center">
                                        <i className="bi bi-question-circle text-info pe-2"></i>
                                        Automatically transcribe any video still missing a transcript at startup, including ones that previously failed.
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
