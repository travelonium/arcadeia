import React, { Component } from 'react';
import Modal from 'react-bootstrap/Modal';
import { VideoPlayer } from './VideoPlayer';
import { extract } from './../utils';

export class MediaViewer extends Component {

    static displayName = MediaViewer.name;

    constructor(props) {
        super(props);
        this.state = {
            sources: [],
            videoJsOptions: {
                aspectRatio: "16:9",
                responsive: true,
                autoplay: true,
                controls: true,
            },
        };
    }

    onHide() {
        this.setState({
            sources: [],
        });
    }

    view(source, player = true) {
        if (source.type === "Video") {
            if (player) {
                this.setState({
                    sources: [source],
                });
            } else {
                window.open("/stream/" + source.id + "/" + source.name, "_blank");
            }
        }
    }

    render() {
        return (
            <div className="media-viewer">
                <Modal show={this.state.sources.length > 0} onHide={this.onHide.bind(this)} backdrop={true} animation={true} size="xl" aria-labelledby="contained-modal-title-vcenter" centered>
                    <Modal.Header closeButton>
                        <Modal.Title>{extract(null, this.state.sources, 0, 'name')}</Modal.Title>
                    </Modal.Header>
                    <VideoPlayer options={this.state.videoJsOptions} sources={this.state.sources.map((item) => ("/stream/" + item.id + "/" + item.name))} />
                </Modal>
            </div>
        );
    }
}
