import Card from 'react-bootstrap/Card';
import React, { Component } from 'react';
import ProgressBar from 'react-bootstrap/ProgressBar';

export class MediaContainer extends Component {
    static displayName = MediaContainer.name;

    constructor(props) {
        super(props);
        this.state = {
            thumbnails: {
                index: 0
            }
        };
        // description="This is a description. It can even span over multiple lines. This one is one example of many."
    }

    componentDidMount() {
        if ((this.props.source.thumbnails != null) && (this.props.source.thumbnails.count > 0)) {
            this.animateInterval = setInterval(() => this.animate(), 500);
        }
    }

    componentWillUnmount() {
        clearInterval(this.animateInterval);
    }

    thumbnail(index) {
        if (this.props.source.type === "Folder") return "/folder.png";
        if (this.props.source.id == null) return null;
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

    render() {
        return (
            <Card onClick={() => this.props.open(this.props.source)} className="media-container">
                <Card.Img variant="top" src={this.thumbnail(this.state.thumbnails.index)} />
                {((this.props.source.thumbnails != null) && (this.props.source.thumbnails.count)) ? <ProgressBar min={1} max={(this.props.source.thumbnails != null) ? this.props.source.thumbnails.count : 0} now={this.state.thumbnails.index + 1} /> : null}
                <Card.ImgOverlay>
                </Card.ImgOverlay>
                <Card.Body>
                    <Card.Title>{this.props.source.name}</Card.Title>
                    <Card.Text>
                        {this.props.source.description}
                    </Card.Text>
                </Card.Body>
            </Card>
        );
    }
}
