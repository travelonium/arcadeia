import Card from 'react-bootstrap/Card';
import React, { Component } from 'react';
import ProgressBar from 'react-bootstrap/ProgressBar';
import Modal from 'react-bootstrap/Modal';
import Badge from 'react-bootstrap/Badge';
import { VideoPlayer } from './VideoPlayer';
import { duration, size } from './../utils';

export class MediaContainer extends Component {
    static displayName = MediaContainer.name;

    constructor(props) {
        super(props);
        this.animateInterval = null;
        this.state = {
            index: -1,
            play: false,
            hover: false,
            source: this.props.source,
            videoJsOptions: {
                aspectRatio: "16:9",
                responsive: true,
                autoplay: true,
                controls: true,
            },
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
        let index = this.state.index;
        let count = this.props.source.thumbnails != null ? this.props.source.thumbnails.count : 0;
        if (count > 0) {
            this.setState({
                index: (index < (count - 1)) ? ++index : 0
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
        console.log(event);
        if (this.state.source.type === "Folder") {
            this.props.open(this.state.source);
        } else {
            this.open(this.state.source);
        }
    }

    onHide() {
        this.setState({
            play: false
        });
    }

    open(source, player = true) {
        if (source.type === "Video") {
            if (player) {
                this.setState({
                    play: true,
                });
            } else {
                window.open("/stream/" + source.id + "/" + source.name, "_blank");
            }
        }
    }

    render() {
        return (
            <div className={"media-container" + (this.props.source.type ? (" " + this.props.source.type.toLowerCase()) : "")}>
                <Card onClick={this.onClick.bind(this)} onMouseOver={this.onMouseOver.bind(this)} onMouseOut={this.onMouseOut.bind(this)} className={(this.state.hover ? " highlighted" : "") } >
                    <div className="card-img-container">
                        <Card.Img src={this.thumbnail(this.state.index)} />
                        <Badge variant="dark" className={"duration " + ((this.props.source.duration > 0) ? "visible" : "invisible")}>{duration(this.props.source.duration)}</Badge>
                    </div>
                    <ProgressBar min={1} max={(this.props.source.thumbnails != null) ? this.props.source.thumbnails.count : 0} now={this.state.index + 1} className={((this.props.source.thumbnails != null) && (this.props.source.thumbnails.count)) ? "visible" : "invisible"} />
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
                <Modal show={((this.state.source.type == "Video") && this.state.play)} onHide={this.onHide.bind(this)} backdrop={true} animation={true} size="xl" aria-labelledby="contained-modal-title-vcenter" centered>
                    <VideoPlayer options={this.state.videoJsOptions} sources={["/stream/" + this.state.source.id + "/" + this.state.source.name]} />
                </Modal>
            </div>
        );
    }
}
