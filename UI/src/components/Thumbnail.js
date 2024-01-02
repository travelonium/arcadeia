import Card from 'react-bootstrap/Card';
import React, { Component } from 'react';
import ProgressBar from 'react-bootstrap/ProgressBar';
import { extract } from './../utils';

export class Thumbnail extends Component {

    static displayName = Thumbnail.name;

    constructor(props) {
        super(props);
        this.animateInterval = null;
        this.state = {
            index: -1,
            loaded: true,
            id: extract(null, this.props, 'source', 'id'),
            type: extract(null, this.props, 'source', 'type'),
            count: extract(0, this.props, 'source', 'thumbnails'),
            children: extract([], this.props, 'source', 'children').reduce((previous, item) => {
                if ((item.type === "Audio") || (item.type === "Photo") || (item.type === "Video")) previous.push(item.id);
                return previous;
            }, []),
        };
    }

    componentDidMount() {
        if ((this.animateInterval == null) && (this.state.id != null) && (this.state.count > 0)) {
            this.animate();
            if (this.state.count > 1) {
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

    thumbnail(id, index) {
        if ((index < 0) || (id === null)) return "/placeholder.png";
        return "/thumbnails/" + id + "/" + (this.props.size ? this.props.size : index) + ".jpg";
    }

    animate() {
        let index = this.state.index;
        if (((!this.props.library.current.viewing) || (index === -1)) && (this.state.count > 0) && (this.state.loaded)) {
            this.setState({
                index: (index < (this.state.count - 1)) ? ++index : 0,
                loaded: false,
            });
        }
    }

    render() {
        return (
            <div className="thumbnail d-flex">
                <div className="thumbnail-icon-wrapper align-self-center text-center position-absolute w-100">
                    { ((this.state.type === "Folder") && (this.state.children.length === 0)) ? <i className="thumbnail-icon bi bi-folder-fill"></i> : <></> }
                </div>
                {
                    ((this.state.type === "Folder") && (this.state.children.length > 0)) ? this.state.children.reverse().map((id, index) => <Card.Img key={index} src={this.thumbnail(id, 0)} />)
                    : <Card.Img src={this.thumbnail(this.state.id, this.state.index)} onLoad={() => this.setState({ loaded: true })} />
                }
                { (this.state.count > 1) ? <ProgressBar variant="info" min={1} max={(this.state.id != null) ? this.state.count : 0} now={this.state.index + 1} className={((this.state.id != null) && (this.state.count)) ? "visible" : "invisible"} /> : <></> }
            </div>
        );
    }
}
