import Card from 'react-bootstrap/Card';
import React, { Component } from 'react';
import ProgressBar from 'react-bootstrap/ProgressBar';


export class Thumbnail extends Component {

    static displayName = Thumbnail.name;

    constructor(props) {
        super(props);
        this.animateInterval = null;
        this.state = {
            index: -1,
            loaded: true,
        };
    }

    componentDidMount() {
        if ((this.animateInterval == null) && (this.props.id != null) && (this.props.count > 0)) {
            this.animate();
            if (this.props.count > 1) {
                this.animateInterval = setInterval(() => this.animate(), 500);
            }
        }
    }

    componentWillUnmount() {
        if (this.animateInterval !== null) {
            clearInterval(this.animateInterval);
            this.animateInterval = null;
        }
    }

    thumbnail(index) {
        if ((index < 0) || (this.props.id === null)) return "/placeholder.png";
        return "/thumbnails/" + this.props.id + "/" + (this.props.size ? this.props.size : index) + ".jpg";
    }

    animate() {
        let index = this.state.index;
        if (((!this.props.library.current.viewing) || (index === -1)) && (this.props.count > 0) && (this.state.loaded)) {
            this.setState({
                index: (index < (this.props.count - 1)) ? ++index : 0,
                loaded: false,
            });
        }
    }

    render() {
        return (
            <div className="thumbnail d-flex">
                <div className="thumbnail-icon-wrapper align-self-center text-center position-absolute w-100">
                    { (this.props.type === "Folder") ? <i className="thumbnail-icon bi bi-folder-fill"></i> : <></> }
                </div>
                <Card.Img src={this.thumbnail(this.state.index)} onLoad={() => this.setState({ loaded: true })} />
                { (this.props.count > 1) ? <ProgressBar variant="info" min={1} max={(this.props.id != null) ? this.props.count : 0} now={this.state.index + 1} className={((this.props.id != null) && (this.props.count)) ? "visible" : "invisible"} /> : <></> }
            </div>
        );
    }
}
