import React, { Component } from 'react';
import Modal from 'react-bootstrap/Modal';
import { EditableText } from './EditableText';
import { VideoPlayer } from './VideoPlayer';
import { PhotoViewer } from './PhotoViewer';
import { extract } from './../utils';
import { Flag } from './Flag';

export class MediaViewer extends Component {

    static displayName = MediaViewer.name;

    constructor(props) {
        super(props);
        this.videoPlayer = React.createRef();
        this.photoViewer = React.createRef();
        this.state = {
            index: -1,
            sources: [],
            videoJsOptions: {
                inactivityTimeout: 5000,
                aspectRatio: "16:9",
                responsive: true,
                autoplay: true,
                controls: true,
            },
            viewerJsOptions: {
                inline: true,
                title: false,
                navbar: false,
                loading: true,
                keyboard: false,
            }
        };
    }

    onHide() {
        this.setState({
            sources: [],
        }, () => {
            if (this.props.onHide !== undefined) {
                this.props.onHide();
            }
        });
    }

    onToggleFavorite(value, event) {
        this.toggle("Favorite");
    }

    onEditing(editing) {
        this.props.library.current.editing = editing;
    }

    view(sources, index, player = true) {
        let source = sources[index];
        if ((source.type !== "Photo") && (source.type !== "Video")) return;
        if (player) {
            this.setState({
                index: index,
                sources: sources,
            }, () => {
                if (this.props.onShow !== undefined) {
                    this.props.onShow();
                }
            });
        } else {
            switch (source.type) {
                case "Photo":
                    window.open("/preview/photo/" + source.id + "/" + source.name, "_blank");
                    break;
                case "Video":
                    window.open("/preview/video/" + source.id + "/" + source.name, "_blank");
                    break;
                default:
                    break;
            }
        }
    }

    viewer(source) {
        const type = extract(null, source, 'type');
        switch (type) {
            case "Photo":
                return <PhotoViewer ref={this.photoViewer} options={this.state.viewerJsOptions} sources={this.state.sources.map((item) => ("/preview/photo/" + item.id + "/" + item.name))} />
            case "Video":
                return <VideoPlayer ref={this.videoPlayer} options={this.state.videoJsOptions} sources={this.state.sources.map((item) => ("/preview/video/" + item.id + "/" + item.name))} />
            default:
                return <></>;
        }
    }

    update(source) {
        let index = this.state.index;
        let sources = this.state.sources;
        sources[index] = {
            ...source,
            // do not update the name until the possible rename operation is through
            name: sources[index].name,
        };
        this.setState({
            sources: sources
        }, () => {
            this.props.onUpdate(source, true, (source) => {
                sources[this.state.index] = source;
                this.setState({
                    sources: sources
                });
            });
        });
    }

    toggle(flag) {
        let source = Object.assign({}, extract({}, this.state.sources, this.state.index));
        let flags = extract([], source, 'flags');
        let index = flags.indexOf(flag);
        if (index !== -1) {
            flags.splice(index, 1);
        } else {
            flags.push(flag);
        }
        source['flags'] = flags;
        this.update(source);
    }

    rename(name) {
        let source = Object.assign({}, extract({}, this.state.sources, this.state.index));
        source.name = name;
        this.update(source);
    }

    render() {
        const source = extract(null, this.state.sources, this.state.index);
        const flags = extract([], this.state.sources, this.state.index, 'flags');
        const name = extract(null, this.state.sources, this.state.index, 'name');
        const favorite = flags.includes('Favorite');
        return (
            <>
                <Modal className="media-viewer" show={this.state.sources.length > 0} onHide={this.onHide.bind(this)} backdrop={true} animation={true} size="xl" aria-labelledby="contained-modal-title-vcenter" centered>
                    <Modal.Header className="flex-row align-items-center me-3" closeButton>
                        <Modal.Title id="contained-modal-title-vcenter" style={{flexGrow: 1, flexShrink: 1, flexBasis: 'auto'}}>
                            <EditableText row={1} value={name} onEditing={this.onEditing.bind(this)} onChange={this.rename.bind(this)} />
                        </Modal.Title>
                    </Modal.Header>
                    <Modal.Body className="">
                        {this.viewer(source)}
                        <div className="flags px-1 ms-4 mt-4">
                            <Flag name="favorite" tooltip={(favorite ? "Unflag" : "Flag") + " Favorite"} value={favorite} set="bi-star-fill" unset="bi-star" onChange={this.onToggleFavorite.bind(this)} />
                        </div>
                    </Modal.Body>
                </Modal>
            </>
        );
    }
}
