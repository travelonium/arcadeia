import Card from 'react-bootstrap/Card';
import React, { Component } from 'react';
import ProgressBar from 'react-bootstrap/ProgressBar';

export class MediaContainer extends Component {
    static displayName = MediaContainer.name;

    constructor(props) {
        super(props);
        this.state = {
            thumbnails: {
                index: 0,
                count: this.props.thumbnails
            }
        };
    }

    componentDidMount() {
        if (this.state.thumbnails.count > 0) {
            this.animateInterval = setInterval(() => this.animate(), 500);
        }
    }

    thumbnail(index) {
        if (this.props.id == null) return null;
        return "/thumbnails/" + this.props.id + "/" + index;
    }

    animate() {
        let index = this.state.thumbnails.index;
        let count = this.state.thumbnails.count;
        if (count > 0) {
            this.setState({
                thumbnails: {
                    index: (index < (count - 1)) ? ++index : 0,
                    count: count
                }
            })
        }
    }

    render() {
        return (
            <Card className="media-container">
                <div className="thumbnail">
                    <Card.Img variant="top" src={this.thumbnail(this.state.thumbnails.index)} />
                    <ProgressBar min={1} max={this.state.thumbnails.count} now={this.state.thumbnails.index + 1} />
                </div>
                <Card.Body>
                    <Card.Title>{this.props.name}</Card.Title>
                    <Card.Text>
                        {this.props.description}
                    </Card.Text>
                </Card.Body>
            </Card>
        );
    }
}
