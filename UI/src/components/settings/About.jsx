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

import Card from 'react-bootstrap/Card';
import { toast } from 'react-toastify';
import { useState, useEffect } from 'react';
import { Container, Row, Col, ListGroup, Spinner, Badge } from 'react-bootstrap';

const LICENSES = {
    "ImageMagick": "https://imagemagick.org/license/",
    "Public Domain": "https://sqlite.org/copyright.html",
};

const THIRD_PARTY = [
    {
        title: "Bundled Tools",
        items: [
            { name: "FFmpeg", url: "https://ffmpeg.org", license: "GPL-3.0-or-later" },
            { name: "whisper.cpp", url: "https://github.com/ggml-org/whisper.cpp", license: "MIT" },
            { name: "Whisper Models", url: "https://github.com/openai/whisper", license: "MIT" },
            { name: "yt-dlp", url: "https://github.com/yt-dlp/yt-dlp", license: "Unlicense" },
            { name: "Apache Solr", url: "https://solr.apache.org", license: "Apache-2.0" },
            { name: "Apache HTTP Server", url: "https://httpd.apache.org", license: "Apache-2.0" },
        ],
    },
    {
        title: "Server Libraries",
        items: [
            { name: ".NET & ASP.NET Core", url: "https://dotnet.microsoft.com", license: "MIT" },
            { name: "SolrNet", url: "https://github.com/SolrNet/SolrNet", license: "Apache-2.0" },
            { name: "Magick.NET", url: "https://github.com/dlemstra/Magick.NET", license: "Apache-2.0" },
            { name: "ImageMagick", url: "https://imagemagick.org", license: "ImageMagick" },
            { name: "Microsoft.Data.Sqlite", url: "https://github.com/dotnet/efcore", license: "MIT" },
            { name: "SQLite", url: "https://sqlite.org", license: "Public Domain" },
            { name: "MessagePack for C#", url: "https://github.com/MessagePack-CSharp/MessagePack-CSharp", license: "MIT" },
        ],
    },
    {
        title: "Web Interface Libraries",
        items: [
            { name: "React", url: "https://react.dev", license: "MIT" },
            { name: "React Bootstrap", url: "https://react-bootstrap.github.io", license: "MIT" },
            { name: "Bootstrap", url: "https://getbootstrap.com", license: "MIT" },
            { name: "Bootstrap Icons", url: "https://icons.getbootstrap.com", license: "MIT" },
            { name: "Bootswatch", url: "https://bootswatch.com", license: "MIT" },
            { name: "Redux Toolkit", url: "https://redux-toolkit.js.org", license: "MIT" },
            { name: "React Redux", url: "https://github.com/reduxjs/react-redux", license: "MIT" },
            { name: "Redux Persist", url: "https://github.com/rt2zz/redux-persist", license: "MIT" },
            { name: "React Router", url: "https://reactrouter.com", license: "MIT" },
            { name: "React Toastify", url: "https://github.com/fkhadra/react-toastify", license: "MIT" },
            { name: "React Window", url: "https://github.com/bvaughn/react-window", license: "MIT" },
            { name: "React Virtualized Auto Sizer", url: "https://github.com/bvaughn/react-virtualized-auto-sizer", license: "MIT" },
            { name: "React SortableJS", url: "https://github.com/SortableJS/react-sortablejs", license: "MIT" },
            { name: "Video.js", url: "https://videojs.com", license: "Apache-2.0" },
            { name: "videojs-vtt-thumbnails", url: "https://github.com/chrisboustead/videojs-vtt-thumbnails", license: "MIT" },
            { name: "videojs-hls-quality-selector", url: "https://github.com/jb-alvarado/videojs-hls-quality-selector", license: "MIT" },
            { name: "Viewer.js", url: "https://fengyuanchen.github.io/viewerjs", license: "MIT" },
            { name: "SignalR", url: "https://github.com/dotnet/aspnetcore/tree/main/src/SignalR", license: "MIT" },
            { name: "Axios", url: "https://axios-http.com", license: "MIT" },
            { name: "Lodash", url: "https://lodash.com", license: "MIT" },
            { name: "Animate.css", url: "https://animate.style", license: "MIT" },
            { name: "Classnames", url: "https://github.com/JedWatson/classnames", license: "MIT" },
            { name: "Immutability Helper", url: "https://github.com/kolodny/immutability-helper", license: "MIT" },
            { name: "path-browserify", url: "https://github.com/browserify/path-browserify", license: "MIT" },
        ],
    },
];

function licenseUrl(license) {
    return LICENSES[license] ?? `https://spdx.org/licenses/${license}.html`;
}

function Link({ href, children }) {
    if (!href) return children;
    return <a href={href} target="_blank" rel="noopener noreferrer">{children}</a>;
}

function Item({ label, children }) {
    return (
        <ListGroup.Item className="d-flex align-items-center px-0">
            <span className="text-muted me-3">{label}</span>
            <span className="ms-auto text-end text-break">{children}</span>
        </ListGroup.Item>
    );
}

function Loading() {
    return (
        <div className="d-flex justify-content-center my-2">
            <Spinner animation="border" size="sm" />
        </div>
    );
}

export default function About() {
    const [about, setAbout] = useState(null);

    // read the application information once on mount
    useEffect(() => {
        let unmounted = false;
        fetch("/api/about", {
            method: "GET",
            headers: {
                accept: "application/json",
            }
        })
        .then((response) => {
            if (!response.ok) throw new Error(`Failed to retrieve the application information: ${response.statusText}`);
            return response.json();
        })
        .then((result) => {
            if (!unmounted) setAbout(result);
        })
        .catch((error) => {
            console.error(error);
            toast.error(error.message);
        });
        return () => {
            unmounted = true;
        };
    }, []);

    const repository = about?.repository;
    const version = about?.version;
    const commit = about?.commit;
    // a version with a pre-release suffix is a build in between the releases
    const release = version && !version.includes('-');
    const commitDate = about?.commitDate ? new Date(about.commitDate).toLocaleDateString(undefined, { dateStyle: "long" }) : null;
    const source = repository && commit ? `${repository}/tree/${commit}` : repository;

    return (
        <Container className="about">
            <Row className="align-items-center mb-2">
                <Col className="my-1">
                    <h2>About</h2>
                </Col>
            </Row>
            <Row>
                <Col>
                    <Container>

                        <Row className="logo align-items-center mb-3">
                            <Col className="d-flex flex-column align-items-center py-3">
                                <svg width="96" height="96" className="animate__animated animate__rotateIn mb-3">
                                    <use xmlnsXlink="http://www.w3.org/1999/xlink" xlinkHref="#logo-emblem"></use>
                                </svg>
                                <svg width="280" height="36">
                                    <use xmlnsXlink="http://www.w3.org/1999/xlink" xlinkHref="#logo-text"></use>
                                </svg>
                                <div className="mt-3">
                                    {
                                        about ? (
                                            <Badge bg="secondary">
                                                {version ? `Version ${version}` : "Unknown Version"}
                                            </Badge>
                                        ) : (
                                            <Spinner animation="border" size="sm" />
                                        )
                                    }
                                </div>
                            </Col>
                        </Row>

                        <Row className="version align-items-center mb-3">
                            <Card className="px-0">
                                <Card.Header className="pe-2">
                                    <Row className="align-items-center">
                                        <Col><b>Version</b></Col>
                                    </Row>
                                </Card.Header>
                                <Card.Body className="py-1">
                                    {!about && <Loading />}
                                    <ListGroup variant="flush" hidden={!about}>
                                        <Item label="Version">
                                            <Link href={release && repository ? `${repository}/releases/tag/${version}` : null}>{version ?? "Unknown"}</Link>
                                        </Item>
                                        <Item label="Commit">
                                            {commit ? <Link href={repository ? `${repository}/commit/${commit}` : null}><code>{commit.substring(0, 7)}</code></Link> : "Unknown"}
                                        </Item>
                                        <Item label="Commit Date">{commitDate ?? "Unknown"}</Item>
                                        <Item label="Runtime">{about?.runtime}</Item>
                                        <Item label="Operating System">{about?.operatingSystem} ({about?.architecture})</Item>
                                    </ListGroup>
                                </Card.Body>
                            </Card>
                        </Row>

                        <Row className="components align-items-center mb-3">
                            <Card className="px-0">
                                <Card.Header className="pe-2">
                                    <Row className="align-items-center">
                                        <Col><b>Components</b></Col>
                                    </Row>
                                </Card.Header>
                                <Card.Body className="py-1">
                                    {!about && <Loading />}
                                    <ListGroup variant="flush">
                                        {
                                            Object.entries(about?.components ?? {}).map(([name, componentVersion]) => (
                                                <Item key={name} label={name}>
                                                    {componentVersion ?? <span className="text-muted">Not Available</span>}
                                                </Item>
                                            ))
                                        }
                                    </ListGroup>
                                </Card.Body>
                            </Card>
                        </Row>

                        <Row className="license align-items-center mb-3">
                            <Card className="px-0">
                                <Card.Header className="pe-2">
                                    <Row className="align-items-center">
                                        <Col><b>License</b></Col>
                                        <Col xs="auto">
                                            <Badge bg="info">{about?.license ?? "AGPL-3.0-or-later"}</Badge>
                                        </Col>
                                    </Row>
                                </Card.Header>
                                <Card.Body>
                                    {about?.copyright && <Card.Text>{about.copyright}</Card.Text>}
                                    <Card.Text>
                                        Arcadeia is free software: you can redistribute it and/or modify it under the terms of the GNU Affero
                                        General Public License as published by the Free Software Foundation, either version 3 of the License,
                                        or (at your option) any later version.
                                    </Card.Text>
                                    <Card.Text>
                                        Arcadeia is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the
                                        implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General
                                        Public License for more details.
                                    </Card.Text>
                                    <Card.Text className="d-flex flex-wrap gap-3">
                                        <Link href={source}>Source Code</Link>
                                        <Link href="https://www.gnu.org/licenses/agpl-3.0.html">GNU Affero General Public License</Link>
                                    </Card.Text>
                                </Card.Body>
                            </Card>
                        </Row>

                        {
                            THIRD_PARTY.map((group) => (
                                <Row key={group.title} className="third-party align-items-center mb-3">
                                    <Card className="px-0">
                                        <Card.Header className="pe-2">
                                            <Row className="align-items-center">
                                                <Col><b>{group.title}</b></Col>
                                            </Row>
                                        </Card.Header>
                                        <Card.Body className="py-1">
                                            <ListGroup variant="flush">
                                                {
                                                    group.items.map((item) => (
                                                        <ListGroup.Item key={item.name} className="d-flex align-items-center px-0">
                                                            <Link href={item.url}>{item.name}</Link>
                                                            <span className="ms-auto">
                                                                <Link href={licenseUrl(item.license)}><Badge bg="secondary">{item.license}</Badge></Link>
                                                            </span>
                                                        </ListGroup.Item>
                                                    ))
                                                }
                                            </ListGroup>
                                        </Card.Body>
                                    </Card>
                                </Row>
                            ))
                        }

                    </Container>
                </Col>
            </Row>
        </Container>
    );
};
