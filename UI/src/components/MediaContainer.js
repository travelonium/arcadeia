import Card from 'react-bootstrap/Card';
import React, { Component } from 'react';
import Badge from 'react-bootstrap/Badge';
import { duration, size, extract } from './../utils';
import { EditableText } from './EditableText';
import { Thumbnail } from './Thumbnail';
import { Flag } from './Flag';

export class MediaContainer extends Component {

    static displayName = MediaContainer.name;

    constructor(props) {
        super(props);
        this.animateInterval = null;
        this.state = {
            index: -1,
            hover: false,
            current: JSON.parse(JSON.stringify(this.props.source)),
            previous: JSON.parse(JSON.stringify(this.props.source)),
        };
    }

    componentDidMount() {
    }

    componentDidUpdate() {
    }

    componentWillUnmount() {
    }

    update(source) {
        this.setState({
            current: source,
        });
        fetch("/library" + source.fullPath, {
            method: "PUT",
            headers: {
                accept: "application/json",
                "Content-Type": "application/json",
            },
            body: JSON.stringify(source)
        })
        .then(async (response) => {
            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message);
            } else {
                return response.json();
            }
        })
        .then((response) => {
            // update the properties of the original instance of the source
            Object.assign(this.props.source, response);
            // update the state and the virtual copies of the source
            this.setState({
                current: JSON.parse(JSON.stringify(response)),
                previous: JSON.parse(JSON.stringify(response)),
            });
        })
        .catch((error) => {
            console.error(error);
            this.setState({
                current: JSON.parse(JSON.stringify(this.state.previous)),
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

    onMouseOver() {
        if (!this.state.hover) {
            this.props.onHighlight(this.state.current);
            this.setState({
                hover: true
            });
        }
    }

    onMouseOut() {
        if (this.state.hover) {
            this.props.onHighlight(null);
            this.setState({
                hover: false
            });
        }
    }

    onClick(event) {
        if (this.state.current.type === "Folder") {
            this.props.onOpen(this.state.current, false);
        } else {
            let player = !(event.shiftKey || event.metaKey);
            this.props.onView(this.state.current, player);
        }
    }

    onToggleFavorite(value, event) {
        this.toggle("Favorite");
    }

    render() {
        const flags = extract([], this.state.current, "flags");
        const favorite = flags.includes('Favorite');
        return (
            <div className={"media-container" + (this.state.current.type ? (" " + this.state.current.type.toLowerCase()) : "")}>
                <Card onClick={this.onClick.bind(this)} onMouseOver={this.onMouseOver.bind(this)} onMouseOut={this.onMouseOut.bind(this)} className={(this.state.hover ? "highlighted" : "") } >
                    <div className="thumbnail-container">
                        <Thumbnail id={this.state.current.id} type={this.state.current.type} count={extract(0, this.props, 'source', 'thumbnails')} />
                        <Badge variant="dark" className={"duration " + ((this.state.current.duration > 0) ? "visible" : "invisible")}>{duration(this.state.current.duration)}</Badge>
                        <div className="flags px-1">
                            <Flag name="favorite" tooltip={(favorite ? "Unflag" : "Flag") + " Favorite"} value={favorite} set="bi-star-fill" unset="bi-star" onChange={this.onToggleFavorite.bind(this)} />
                        </div>
                    </div>
                    <Card.Body className="d-flex flex-column">
                        <Card.Title title={this.state.current.name} style={{flexShrink: 1, flexGrow: 1}}>
                            <EditableText value={this.state.current.name} onChange={this.rename.bind(this)} />
                        </Card.Title>
                        {/*
                        <Card.Text title={this.state.current.description} style={{flexShrink: 1, flexGrow: 1}}>
                            {this.state.current.description}
                        </Card.Text>
                        */}
                    </Card.Body>
                    <div className="d-flex flex-row p-1" style={{flexShrink: 0}}>
                        <div className="pl-1" style={{flexGrow: 1}}>
                            <small>{(this.state.current.size) ? size(this.state.current.size) : <span>&nbsp;</span>}</small>
                        </div>
                    </div>
                </Card>
            </div>
        );
    }
}
