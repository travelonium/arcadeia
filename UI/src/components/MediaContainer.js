import Card from 'react-bootstrap/Card';
import React, { Component } from 'react';
import Badge from 'react-bootstrap/Badge';
import Popover from 'react-bootstrap/Popover';
import OverlayTrigger from 'react-bootstrap/OverlayTrigger';
import { duration, size, extract, clone } from './../utils';
import { EditableText } from './EditableText';
import { Thumbnail } from './Thumbnail';
import { Flag } from './Flag';
import cx from 'classnames';

export class MediaContainer extends Component {

    static displayName = MediaContainer.name;

    constructor(props) {
        super(props);
        this.animateInterval = null;
        this.state = {
            current: clone(this.props.source),
            previous: clone(this.props.source),
        };
    }

    componentDidUpdate(prevProps) {
        // console.log(prevProps);
    }

    update(source) {
        this.setState({
            current: source,
        }, () => {
            this.props.onUpdate(source, false, (source, succeeded) => {
                this.setState({
                    current: succeeded ? clone(source) : clone(this.state.previous),
                    previous: succeeded ? clone(source) : clone(this.state.previous),
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
        if (this.state.current.type === "Folder") {
            this.props.onOpen(this.state.current, false);
        } else {
            let player = !(event.shiftKey || event.metaKey || (event.button === 1));
            this.props.onView(this.state.current, this.props.index, player);
            // update the views count asynchronously
            this.update({
                ...this.state.current,
                views: this.state.current.views + 1,
            });
        }
    }

    onAuxClick(event) {
        if ((this.state.current.type !== "Folder") && (event.button === 1)) {
            event.preventDefault();
            event.stopPropagation();
            this.props.onView(this.state.current, this.props.index, false);
            // update the views count asynchronously
            this.update({
                ...this.state.current,
                views: this.state.current.views + 1,
            });
        }
    }

    onToggleFavorite(value, event) {
        this.toggle("Favorite");
    }

    onEditing(editing) {
        this.props.library.current.editing = editing;
    }

    preview(props) {
        let name = extract(0, this.state.current, 'name');
        let width = extract(0, this.state.current, 'width');
        let height = extract(0, this.state.current, 'height');
        if (!height || !width || (this.state.current.type !== "Photo")) return <div></div>;
        let src = "/thumbnails/" + this.props.source.id + "/large.jpg";
        return (
            <Popover {...props} id="popover-preview" className="photo-preview" >
                <Popover.Body>
                    <img src={src} alt={name + ' Preview'} />
                </Popover.Body>
            </Popover>
        );
    }

    render() {
        const source = this.state.current;
        const type = extract(null, source, 'type');
        const flags = extract([], source, 'flags');
        const favorite = flags.includes('Favorite');
        let href = source.fullPath;
        switch (type) {
            case "Photo":
                href = "/preview/photo/" + source.id + "/" + source.name;
                break;
            case "Video":
                href = "/preview/video/" + source.id + "/" + source.name;
                break;
        }
        return (
            <a href={href} className={"media-container" + (source.type ? (" " + source.type.toLowerCase()) : "")} onClick={(event) => event.preventDefault()} >
                <Card onClick={this.onClick.bind(this)} onAuxClick={this.onAuxClick.bind(this)} >
                    <OverlayTrigger placement="auto" delay={{ show: 1000, hide: 0 }} overlay={this.preview.bind(this)}>
                        <div className="thumbnail-container">
                            <Thumbnail id={source.id} type={source.type} count={extract(0, this.props, 'source', 'thumbnails')} library={this.props.library} />
                            <Badge variant="dark" className={cx("duration", (source.duration > 0) ? "visible" : "invisible")}>{duration(source.duration)}</Badge>
                            <Badge variant="dark" className={cx("extension", source.extension ? "visible" : "invisible")}>{source.extension}</Badge>
                            <div className="flags px-1">
                                <Flag name="favorite" tooltip={(favorite ? "Unflag" : "Flag") + " Favorite"} value={favorite} set="bi-star-fill" unset="bi-star" onChange={this.onToggleFavorite.bind(this)} />
                            </div>
                        </div>
                    </OverlayTrigger>
                    <Card.Body className="d-flex flex-column">
                        <EditableText name="Name" className="card-title h5 name text-overflow-ellipsis" row={1} value={source.name} onEditing={this.onEditing.bind(this)} onChange={this.rename.bind(this)} />
                        <EditableText name="Description" className="card-text h6 description" row={2} value={source.description} onEditing={this.onEditing.bind(this)} onChange={this.redescribe.bind(this)} />
                    </Card.Body>
                    <div className="d-flex flex-row p-1" style={{flexShrink: 0}}>
                        <div className="d-flex align-items-center ps-1" style={{flexGrow: 1}}>
                        {
                            ((source.type === "Video") || (source.type === "Audio") || (source.type === "Photo")) ? <>
                            <i className="bi bi-eye-fill me-1"></i>
                            <small>{source.views}</small>
                            </> : <></>
                        }
                        </div>
                        <div className="d-flex align-items-center pe-1">
                            <small>{(source.size) ? size(source.size) : <span>&nbsp;</span>}</small>
                        </div>
                    </div>
                </Card>
            </a>
        );
    }
}
