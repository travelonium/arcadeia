import Card from 'react-bootstrap/Card';
import React, { Component } from 'react';
import ProgressBar from 'react-bootstrap/ProgressBar';

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
        if (this.animateInterval == null) {
            if ((this.props.source.thumbnails != null) && (this.props.source.thumbnails.count > 0)) {
                this.animateInterval = setInterval(() => this.animate(), 500);
            }
        }
    }

    componentDidUpdate() {
        if (this.animateInterval == null) {
            // start or restart the timer if necessary
            if ((this.props.source.thumbnails != null) && (this.props.source.thumbnails.count > 0)) {
                this.animateInterval = setInterval(() => this.animate(), 500);
            }
        } else {
            // stop the timer if necessary
            if ((this.props.source.thumbnails === null) || (this.props.source.thumbnails.count === 0)) {
                clearInterval(this.animateInterval);
                this.animateInterval = null;
            }
        }
    }

    componentWillUnmount() {
        clearInterval(this.animateInterval);
        this.animateInterval = null;
    }

    thumbnail(index) {
        if (this.props.source.type === "Folder") return "/folder.png";
        if ((index < 0) || (this.props.source.id == null)) return "/placeholder.png";
        return "/thumbnails/" + this.props.source.id + "/" + index;
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
        this.setState({
            hover: true
        });
    }

    onMouseOut() {
        this.setState({
            hover: false
        });
    }

    render() {
        return (
            <Card onClick={() => this.props.open(this.props.source)} onMouseOver={this.onMouseOver.bind(this)} onMouseOut={this.onMouseOut.bind(this)} className={"media-container" + (this.state.hover ? " shadow-sm" : "")} >
                <Card.Img variant="top" src={this.thumbnail(this.state.thumbnails.index)} />
                <ProgressBar min={1} max={(this.props.source.thumbnails != null) ? this.props.source.thumbnails.count : 0} now={this.state.thumbnails.index + 1} className={((this.props.source.thumbnails != null) && (this.props.source.thumbnails.count)) ? "visible" : "invisible"} />
                <Card.ImgOverlay>
                </Card.ImgOverlay>
                <Card.Body>
                    <Card.Title className="text-center text-truncate" >{this.props.source.name}</Card.Title>
                    <Card.Text>
                        {this.props.source.description}
                    </Card.Text>
                </Card.Body>
            </Card>
        );
    }
}
