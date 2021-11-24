import React, { Component } from 'react';
import { EditableText } from './EditableText';
import { VideoPlayer } from './VideoPlayer';
import Modal from 'react-bootstrap/Modal';
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

    rename(name) {
        let source = Object.assign({}, extract({}, this.state.sources, 0));
        source.name = name;
        this.setState({
            sources: [source]
        }, () => {
            this.props.onUpdate(source, true, (source) => {
                this.setState({
                    sources: [source]
                });
            });
        });
    }

    render() {
        const name = extract(null, this.state.sources, 0, 'name');
        return (
            <>
                <Modal className="media-viewer" show={this.state.sources.length > 0} onHide={this.onHide.bind(this)} backdrop={true} animation={true} size="xl" aria-labelledby="contained-modal-title-vcenter" centered>
                    <Modal.Header className="flex-row align-items-center mr-3" closeButton>
                        <Modal.Title id="contained-modal-title-vcenter" style={{flexGrow: 1, flexShrink: 1, flexBasis: 'auto'}}>
                            <EditableText row="1" value={name} onChange={this.rename.bind(this)} />
                        </Modal.Title>
                    </Modal.Header>
                    <VideoPlayer options={this.state.videoJsOptions} sources={this.state.sources.map((item) => ("/stream/" + item.id + "/" + item.name))} />
                </Modal>
            </>
        );
    }
}
