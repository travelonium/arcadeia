import Card from 'react-bootstrap/Card';
import React, { Component } from 'react';
import Badge from 'react-bootstrap/Badge';
import { Thumbnail } from './Thumbnail';
import { duration, size, extract } from './../utils';

export class MediaContainer extends Component {

    static displayName = MediaContainer.name;

    constructor(props) {
        super(props);
        this.animateInterval = null;
        this.state = {
            index: -1,
            hover: false,
            source: JSON.parse(JSON.stringify(this.props.source)),
            previous: JSON.parse(JSON.stringify(this.props.source)),
        };
    }

    componentDidMount() {
    }

    componentDidUpdate() {
    }

    componentWillUnmount() {
    }

    apply(source) {
        this.setState({
            source: source,
        });
        fetch("/library" + source.fullPath, {
            method: "PUT",
            headers: {
                accept: "application/json",
                "Content-Type": "application/json",
            },
            body: JSON.stringify(source)
        })
        .then((response) => {
            if (response.ok) {
                return response.json();
            } else {
                throw new Error(response.json());
            }
        })
        .then((response) => {
            // update the properties of the original instance of the source
            Object.assign(this.props.source, response);
            // update the state and the virtual copies of the source
            this.setState({
                source: JSON.parse(JSON.stringify(response)),
                previous: JSON.parse(JSON.stringify(response)),
            });
        })
        .catch((error) => {
            this.setState({
                source: JSON.parse(JSON.stringify(this.state.previous)),
            });
        });
    }

    toggle(flag) {
        let source = this.state.source;
        let index = source.flags.indexOf(flag);
        if (index !== -1) {
            source.flags.splice(index, 1);
        } else {
            source.flags.push(flag);
        }
        this.apply(source);
    }

    onMouseOver() {
        if (! this.state.hover) {
            this.props.highlight(this.state.source);
            this.setState({
                hover: true
            });
        }
    }

    onMouseOut() {
        if (this.state.hover) {
            this.props.highlight(null);
            this.setState({
                hover: false
            });
        }
    }

    onClick(event) {
        if (this.state.source.type === "Folder") {
            this.props.open(this.state.source, false);
        } else {
            let player = !(event.shiftKey || event.metaKey);
            this.props.view(this.state.source, player);
        }
    }

    onToggleFavorite(event) {
        event.stopPropagation();
        this.toggle("Favorite");
    }

    render() {
        return (
            <div className={"media-container" + (this.state.source.type ? (" " + this.state.source.type.toLowerCase()) : "")}>
                <Card onClick={this.onClick.bind(this)} onMouseOver={this.onMouseOver.bind(this)} onMouseOut={this.onMouseOut.bind(this)} className={(this.state.hover ? "highlighted" : "") } >
                    <div className="thumbnail-container">
                        <Thumbnail id={this.state.source.id} type={this.state.source.type} count={extract(0, this.props, 'source', 'thumbnails', 'count')} />
                        <Badge variant="dark" className={"duration " + ((this.state.source.duration > 0) ? "visible" : "invisible")}>{duration(this.state.source.duration)}</Badge>
                        <div className="flags">
                            <span onClick={this.onToggleFavorite.bind(this)} className={"flag favorite" + (this.state.source.flags.includes('Favorite') ? " set" : "")}></span>
                        </div>
                    </div>
                    <Card.Body className="d-flex flex-column">
                        <Card.Title style={{flexShrink: 1, flexGrow: 1}}>
                            {this.state.source.name}
                        </Card.Title>
                        <Card.Text style={{flexShrink: 1, flexGrow: 1}}>
                            {this.state.source.description}
                        </Card.Text>
                    </Card.Body>
                    <div className="d-flex flex-row p-1" style={{flexShrink: 0}}>
                        <div className="pl-1" style={{flexGrow: 1}}>
                            <small>{(this.state.source.size) ? size(this.state.source.size) : <span>&nbsp;</span>}</small>
                        </div>
                    </div>
                </Card>
            </div>
        );
    }
}
