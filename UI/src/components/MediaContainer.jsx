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
import { Flag } from './toolbar/Flag';
import { connect } from "react-redux";
import Card from 'react-bootstrap/Card';
import { Thumbnail } from './Thumbnail';
import React, { Component } from 'react';
import update from 'immutability-helper';
import Badge from 'react-bootstrap/Badge';
import Popover from 'react-bootstrap/Popover';
import { EditableText } from './EditableText';
import { Col, Container, Row } from 'react-bootstrap';
import OverlayTrigger from 'react-bootstrap/OverlayTrigger';
import { duration, size, extract, clone, withRouter } from '../utils';

export class MediaContainer extends Component {

    static displayName = MediaContainer.name;
    static defaultProps = {
        view: "card",
    }

    constructor(props) {
        super(props);
        this.animateInterval = null;
        this.state = {
            current: clone(this.props.source),
            previous: clone(this.props.source),
        };
    }

    componentDidUpdate(prevProps) {
    }

    set(source, callback = undefined) {
        this.setState(prevState => {
            return {
                current: update(prevState.current, {
                    $merge: source
                })
            }
        }, () => {
            if (callback !== undefined) {
                callback(this.state.current);
            }
        });
    }

    update(source, callback = undefined) {
        this.setState({
            current: source,
        }, () => {
            this.props.onUpdate(source, false, (source, succeeded) => {
                this.setState({
                    current: succeeded ? clone(source) : clone(this.state.previous),
                    previous: succeeded ? clone(this.state.current) : clone(this.state.previous),
                }, () => {
                    if (callback !== undefined) {
                        callback(this.state.current);
                    }
                });
            });
        });
    }

    toggle(flag) {
        let source = Object.assign({}, this.state.current);
        let flags = extract([], source, 'flags');
        let index = flags.indexOf(flag);
        if (index !== -1) {
            flags.splice(index, 1);
        } else {
            flags.push(flag);
        }
        source['flags'] = flags;
        this.update(source);
    }

    rename(name) {
        let source = Object.assign({}, this.state.current);
        source.name = name;
        this.update(source);
    }

    redescribe(description) {
        let source = Object.assign({}, this.state.current);
        source.description = description;
        this.update(source);
    }

    onClick(event) {
        let player = !(event.shiftKey || event.metaKey || (event.button === 1));
        if (this.state.current.type === "Folder") {
            this.props.onOpen(this.state.current, false);
        } else {
            this.props.onView(this.state.current, this.props.index, player);
        }
    }

    onAuxClick(event) {
        if ((this.state.current.type !== "Folder") && (event.button === 1)) {
            event.preventDefault();
            event.stopPropagation();
            this.props.onView(this.state.current, this.props.index, false);
        }
    }

    onToggleFavorite(value, event) {
        this.toggle("Favorite");
    }

    onDuplicates(event) {
        event.preventDefault();
        event.stopPropagation();
        const params = new URLSearchParams(this.props.location.search);
        params.set('duplicates', true);
        const source = this.state.current;
        const path = source.fullPath;
        const url = encodeURI(path + "?" + params.toString());
        this.props.navigate(url);
    }

    onEditing(editing) {
        this.props.library.current.editing = editing;
    }

    preview(props) {
        let name = extract(0, this.state.current, 'name');
        let width = extract(0, this.state.current, 'width');
        let height = extract(0, this.state.current, 'height');
        if (!height || !width || (this.state.current.type !== "Photo")) return <div></div>;
        let src = "/api/thumbnails/" + this.props.source.id + "/large.jpg";
        return (
            <Popover {...props} id="popover-preview" className="photo-preview" >
                <Popover.Body>
                    <img src={src} alt={name + ' Preview'} />
                </Popover.Body>
            </Popover>
        );
    }

    card(source) {
        const flags = source?.flags ?? [];
        const favorite = flags.includes('Favorite');
        const children = (source?.children ?? []).reverse().filter((item) => ((item.type === "Audio") || (item.type === "Photo") || (item.type === "Video")) && item.thumbnails > 0).map((item) => item.id);
        return (
            <Card className={cx((children.length > 0) ? "" : "childless")} onClick={this.onClick.bind(this)} onAuxClick={this.onAuxClick.bind(this)} >
                <OverlayTrigger placement="auto" delay={{ show: 1000, hide: 0 }} overlay={this.preview.bind(this)}>
                    <div className="thumbnail-container">
                        <Thumbnail source={source} children={children} library={this.props.library} />
                        <Container className="properties" fluid>
                            <Row className="gx-2">
                                <Col className="d-flex">
                                {
                                    (source.type === "Folder") ? <i className="type bi bi-folder-fill"></i> :
                                    (source.extension) ? <Badge className="btn btn-primary extension" title="Extension">{source.extension}</Badge> : <></>
                                }
                                </Col>
                            {
                                (source.duplicates > 0) ?
                                <Col className="d-flex" xs="auto">
                                    <Badge className="btn btn-warning duplicates" title="Duplicates" onClick={this.onDuplicates.bind(this)}>{source.duplicates}</Badge>
                                </Col> :
                                <></>
                            }
                            {
                                (source.width && source.height) ?
                                <Col className="d-flex" xs="auto">
                                    <Badge className={cx("resolution btn", (source.duration > 0) ? "btn-dark" : "btn-primary")} title="Resolution">{`${source.width} × ${source.height}`}</Badge>
                                </Col> :
                                <></>
                            }
                            {
                                (source.duration > 0) ?
                                <Col className="d-flex" xs="auto">
                                    <Badge className="duration btn btn-primary" title="Duration">{duration(source.duration)}</Badge>
                                </Col> :
                                <></>
                            }
                            </Row>
                        </Container>
                        <div className="flags px-1">
                            <Flag name="favorite" tooltip={(favorite ? "Unflag" : "Flag") + " Favorite"} value={favorite} set="bi-star-fill" unset="bi-star" onChange={this.onToggleFavorite.bind(this)} />
                        </div>
                    </div>
                </OverlayTrigger>
                <div className="content-container d-flex flex-column">
                    <Card.Body className="d-flex flex-column flex-grow-1">
                        <EditableText name="Name" className="card-title h5 name text-overflow-ellipsis" row={1} value={source.name} onEditing={this.onEditing.bind(this)} onChange={this.rename.bind(this)} />
                        <EditableText name="Description" className="card-text h6 description" row={2} value={source.description} onEditing={this.onEditing.bind(this)} onChange={this.redescribe.bind(this)} />
                    </Card.Body>
                    <Card.Footer className="d-flex flex-row p-1" style={{flexShrink: 0}}>
                        <Container className="gx-2" fluid>
                            <Row>
                                <Col className="d-flex align-items-center justify-content-start" title="Views">
                                {
                                    ((source.type === "Video") || (source.type === "Audio") || (source.type === "Photo")) ? <>
                                    <i className="d-flex bi bi-eye-fill me-1" />
                                    <small>{source.views}</small>
                                    </> : <></>
                                }
                                </Col>
                                <Col className="d-flex align-items-center justify-content-end" title="Size">
                                    <small>{(source.size) ? size(source.size) : <span>&nbsp;</span>}</small>
                                </Col>
                            </Row>
                        </Container>
                    </Card.Footer>
                </div>
            </Card>
        )
    }

    thumbnail(source) {
        const flags = extract([], source, 'flags');
        const favorite = flags.includes('Favorite');
        const children = (source?.children ?? []).reverse().filter((item) => ((item.type === "Audio") || (item.type === "Photo") || (item.type === "Video")) && item.thumbnails > 0).map((item) => item.id);
        return (
            <Card className={cx((children.length > 0) ? "" : "childless")} onClick={this.onClick.bind(this)} onAuxClick={this.onAuxClick.bind(this)} >
                <OverlayTrigger placement="auto" delay={{ show: 1000, hide: 0 }} overlay={this.preview.bind(this)}>
                    <div className="thumbnail-container">
                        <Thumbnail source={source} children={children} library={this.props.library} />
                        <Container className="properties" fluid>
                            <Row className="gx-2">
                                <Col className="d-flex">
                                {
                                    (source.type === "Folder") ? <i className="type bi bi-folder-fill"></i> :
                                    (source.extension) ? <Badge className="btn btn-primary extension" title="Extension">{source.extension}</Badge> : <></>
                                }
                                </Col>
                            {
                                (source.duplicates > 0) ?
                                <Col className="d-flex" xs="auto">
                                    <Badge className="btn btn-warning duplicates" title="Duplicates" onClick={this.onDuplicates.bind(this)}>{source.duplicates}</Badge>
                                </Col> :
                                <></>
                            }
                            {
                                (source.width && source.height) ?
                                <Col className="d-flex" xs="auto">
                                    <Badge className={cx("resolution btn", (source.duration > 0) ? "btn-dark" : "btn-primary")} title="Resolution">{`${source.width} × ${source.height}`}</Badge>
                                </Col> :
                                <></>
                            }
                            {
                                (source.duration > 0) ?
                                <Col className="d-flex" xs="auto">
                                    <Badge className="duration btn btn-primary" title="Duration">{duration(source.duration)}</Badge>
                                </Col> :
                                <></>
                            }
                            </Row>
                        </Container>
                        <div className="flags px-1">
                            <Flag name="favorite" tooltip={(favorite ? "Unflag" : "Flag") + " Favorite"} value={favorite} set="bi-star-fill" unset="bi-star" onChange={this.onToggleFavorite.bind(this)} />
                        </div>
                    </div>
                </OverlayTrigger>
                <Card.ImgOverlay className="content-container flex-column">
                    <EditableText name="Name" className="card-title h5 name text-overflow-ellipsis" row={1} value={source.name} onEditing={this.onEditing.bind(this)} onChange={this.rename.bind(this)} />
                </Card.ImgOverlay>
            </Card>
        );
    }

    view(source, view) {
        switch (view) {
            case "card":
                return this.card(source);

            case "thumbnail":
                return this.thumbnail(source);

            default:
                return this.card(source);
        }
    }

    render() {
        const source = this.state.current;
        const view = this.props.view.toLowerCase();
        const type = extract(null, source, 'type');
        let href = source.fullPath;
        switch (type) {
            case "Photo":
                href = "/api/preview/photo/" + source.id + "/" + source.name;
                break;
            case "Video":
                href = "/api/preview/video/" + source.id + "/" + source.name;
                break;
            default:
                break
        }
        return (
            <a href={href} className={cx("media-container" + (source.type ? (" " + source.type.toLowerCase()) : ""), (view.toLowerCase() + "-view"))} onClick={(event) => event.preventDefault()} >
            {
                this.view(source, view)
            }
            </a>
        );
    }
}

const mapStateToProps = (state) => ({});

export default connect(mapStateToProps, null, null, { forwardRef: true })(
    withRouter(MediaContainer)
);
