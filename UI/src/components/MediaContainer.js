import Card from 'react-bootstrap/Card';
import React, { Component } from 'react';
import ProgressBar from 'react-bootstrap/ProgressBar';
import VisibilitySensor from 'react-visibility-sensor';
import Badge from 'react-bootstrap/Badge';
import { duration } from './../utils';

export class MediaContainer extends Component {
    static displayName = MediaContainer.name;

    constructor(props) {
        super(props);
        this.animateInterval = null;
        this.state = {
            hover: false,
            visible: false,
            thumbnails: {
                index: -1
            }
        };
    }

    componentDidMount() {
    }

    componentDidUpdate() {
        if ((this.animateInterval != null) && ((this.props.source.thumbnails === null) || (this.props.source.thumbnails.count === 0))) {
            // no thumbnails available to display
            clearInterval(this.animateInterval);
            this.animateInterval = null;
            return;
        }
        if (this.state.visible) {
            // the component is visible now, start or restart the timer if necessary
            if ((this.animateInterval == null) && (this.props.source.thumbnails != null) && (this.props.source.thumbnails.count > 0)) {
                // console.log("Started: " + this.props.source.id);
                this.animateInterval = setInterval(() => this.animate(), 500);
            }
        } else {
            // the component is no longer visible, stop the timer if necessary
            if (this.animateInterval != null) {
                // console.log("Stopped: " + this.props.source.id);
                clearInterval(this.animateInterval);
                this.animateInterval = null;
            }
        }
    }

    componentWillUnmount() {
        clearInterval(this.animateInterval);
        this.animateInterval = null;
    }

    componentVisibilityChanged(visible) {
        this.setState({ visible: visible })
    }

    thumbnail(index) {
        let source = this.props.source;
        if (source.type === "Folder") return "/folder.png";
        if ((index < 0) || (source.id == null) || (source.thumbnails == null) || (source.thumbnails.count == 0)) return "/placeholder.png";
        return "/thumbnails/" + source.id + "/" + index + ".jpg";
    }

    animate() {
        let count = this.props.source.thumbnails != null ? this.props.source.thumbnails.count : 0;
        let index = this.state.thumbnails.index;
        if ((this.state.visible) && (count > 0)) {
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
            <VisibilitySensor partialVisibility onChange={(isVisible) => { this.componentVisibilityChanged(isVisible) }}>
                <Card onClick={() => this.props.open(this.props.source)} onMouseOver={this.onMouseOver.bind(this)} onMouseOut={this.onMouseOut.bind(this)} className={"media-container" + (this.state.hover ? " highlighted" : "")} >
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
                </Card>
            </VisibilitySensor>
        );
    }
}
