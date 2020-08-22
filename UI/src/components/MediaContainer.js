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
        };
    }

    componentDidMount() {
    }

    componentDidUpdate() {
    }

    componentWillUnmount() {
    }

    onMouseOver() {
        if (! this.state.hover) {
            this.props.highlight(this.props.source);
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
        if (this.props.source.type === "Folder") {
            this.props.open(this.props.source, false);
        } else {
            let player = !(event.shiftKey || event.metaKey);
            this.props.view(this.props.source, player);
        }
    }

    render() {
        return (
            <div className={"media-container" + (this.props.source.type ? (" " + this.props.source.type.toLowerCase()) : "")}>
                <Card onClick={this.onClick.bind(this)} onMouseOver={this.onMouseOver.bind(this)} onMouseOut={this.onMouseOut.bind(this)} className={(this.state.hover ? "highlighted" : "") } >
                    <div className="thumbnail-container">
                        <Thumbnail id={this.props.source.id} type={this.props.source.type} count={extract(0, this.props, 'source', 'thumbnails', 'count')} />
                        <Badge variant="dark" className={"duration " + ((this.props.source.duration > 0) ? "visible" : "invisible")}>{duration(this.props.source.duration)}</Badge>
                    </div>
                    <Card.Body className="d-flex flex-column">
                        <Card.Title style={{flexShrink: 1, flexGrow: 1}}>
                            {this.props.source.name}
                        </Card.Title>
                        <Card.Text style={{flexShrink: 1, flexGrow: 1}}>
                            {this.props.source.description}
                        </Card.Text>
                    </Card.Body>
                    <div className="d-flex flex-row p-1" style={{flexShrink: 0}}>
                        <div className="pl-1" style={{flexGrow: 1}}>
                            <small>{(this.props.source.size) ? size(this.props.source.size) : <span>&nbsp;</span>}</small>
                        </div>
                    </div>
                </Card>
            </div>
        );
    }
}
