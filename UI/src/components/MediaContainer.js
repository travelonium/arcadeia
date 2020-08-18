import Card from 'react-bootstrap/Card';
import React, { Component } from 'react';
import ProgressBar from 'react-bootstrap/ProgressBar';
import Badge from 'react-bootstrap/Badge';
import { duration, size } from './../utils';

export class MediaContainer extends Component {
    static displayName = MediaContainer.name;

    constructor(props) {
        super(props);
        this.animateInterval = null;
        this.state = {
            hover: false,
            thumbnails: {
                index: -1
            }
        };
    }

    componentDidMount() {
        if ((this.animateInterval == null) && (this.props.source.thumbnails != null) && (this.props.source.thumbnails.count > 0)) {
            // console.log("Started: " + this.props.source.id);
            this.animateInterval = setInterval(() => this.animate(), 500);
        }
    }

    componentDidUpdate() {
    }

    componentWillUnmount() {
        if (this.animateInterval !== null) {
            // console.log("Stopped: " + this.props.source.id);
            clearInterval(this.animateInterval);
            this.animateInterval = null;
        }
    }

    thumbnail(index) {
        let source = this.props.source;
        if (source.type === "Folder") return "/folder.png";
        if ((index < 0) || (source.id === null) || (source.thumbnails === null) || (source.thumbnails.count === 0)) return "/placeholder.png";
        return "/thumbnails/" + source.id + "/" + index + ".jpg";
    }

    animate() {
        let count = this.props.source.thumbnails != null ? this.props.source.thumbnails.count : 0;
        let index = this.state.thumbnails.index;
        if (count > 0) {
            this.setState({
                thumbnails: {
                    index: (index < (count - 1)) ? ++index : 0
                }
            })
        }
    }

    onMouseOver() {
        if (! this.state.hover) this.props.highlight(this.props.source);
        this.setState({
            hover: true
        });
    }

    onMouseOut() {
        if (this.state.hover) this.props.highlight(null);
        this.setState({
            hover: false
        });
    }

    onClick(event) {
        this.props.open(this.props.source);
    }

    render() {
        return (
            <Card onClick={this.onClick.bind(this)} onMouseOver={this.onMouseOver.bind(this)} onMouseOut={this.onMouseOut.bind(this)} className={"media-container" + (this.state.hover ? " highlighted" : "")} >
                <Card.Img variant="top" src={this.thumbnail(this.state.thumbnails.index)} />
                <ProgressBar min={1} max={(this.props.source.thumbnails != null) ? this.props.source.thumbnails.count : 0} now={this.state.thumbnails.index + 1} className={((this.props.source.thumbnails != null) && (this.props.source.thumbnails.count)) ? "visible" : "invisible"} />
                <Card.ImgOverlay>
                    <Badge variant="dark" className={(this.props.source.duration > 0) ? "visible" : "invisible"}>{duration(this.props.source.duration)}</Badge>
                </Card.ImgOverlay>
                <Card.Body>
                    <p className="font-weight-bold text-center" >{this.props.source.name}</p>
                    <Card.Text>
                        {this.props.source.description}
                    </Card.Text>
                </Card.Body>
                <div className="d-flex flex-row p-1">
                    <div className="pl-1" style={{flexGrow: 1}}>
                        <small>{(this.props.source.size) ? size(this.props.source.size) : <span>&nbsp;</span>}</small>
                    </div>
                </div>
            </Card>
        );
    }
}
