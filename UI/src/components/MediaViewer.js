import update from 'immutability-helper';
import React, { Component } from 'react';
import Modal from 'react-bootstrap/Modal';
import { VideoPlayer } from './VideoPlayer';
import { PhotoViewer } from './PhotoViewer';
import { EditableText } from './EditableText';
import { reset } from '../features/search/slice';
import { useParams } from "react-router-dom";
import { extract, clone } from './../utils';
import { connect } from "react-redux";
import { Flag } from './Flag';

class MediaViewer extends Component {

    static displayName = MediaViewer.name;

    constructor(props) {
        super(props);
        this.videoPlayer = React.createRef();
        this.photoViewer = React.createRef();
        this.state = {
            index: -1,
            sources: [],
            origin: null,
            expanded: false,
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

    onShow() {}

    onHide() {
        if (this.state.origin) {
            this.props.dispatch(reset(this.state.origin));
            window.history.pushState({path: this.state.origin}, "", this.state.origin);
        } else {
            throw Error("No origin has been set on the MediaViewer instance.");
        }
        this.setState({
            sources: [],
            origin: null
        }, () => {
            if (this.props.onHide !== undefined) this.props.onHide();
        });
    }

    onToggleFavorite(value, event) {
        this.toggle("Favorite");
    }

    onToggleExpand(value, event) {
        this.setState({
            expanded: value
        });
    }

    onEditing(editing) {
        this.props.library.current.editing = editing;
    }

    view(sources, index, player = true, history = false, origin = null) {
        let source = sources[index];
        if (!origin) origin = this.state.origin;
        if ((source.type !== "Photo") && (source.type !== "Video")) return;
        if (player) {
            const search = origin.split('?')[1];
            const parent = origin.match(/.*\//g)[0];
            const path = parent + source.name + '?' + search;
            this.setState({
                index: index,
                sources: sources,
                origin: parent + '?' + search
            }, () => {
                if (!history) window.history.pushState({path: path}, "", path);
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

    hide() {
        this.setState({
            sources: [],
        });
    }

    viewer(source) {
        const type = extract(null, source, 'type');
        switch (type) {
            case "Photo":
                return <PhotoViewer ref={this.photoViewer} options={this.state.viewerJsOptions} sources={this.state.sources} />
            case "Video":
                return <VideoPlayer ref={this.videoPlayer} options={this.state.videoJsOptions} sources={this.state.sources} />
            default:
                return <></>;
        }
    }

    update(source) {
        let index = this.state.index;
        let original = extract(undefined, this.state.sources, index);
        if (original) {
            // preliminarily update the state with the new value until we receive the response
            this.setState({
                sources: update(this.state.sources, {
                    [index]: {$set: source}
                })
            }, () => {
                this.props.onUpdate(source, false, (source, succeeded) => {
                    // now that we have the response we override the preliminary value
                    this.setState({
                        sources: update(this.state.sources, {
                            [index]: {$set: (succeeded ? clone(source) : clone(original))}
                        })
                    });
                });
            })
        } else {
            console.error("Invalid source index!");
        }
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
        let source = clone(extract({}, this.state.sources, this.state.index));
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
                <Modal className="media-viewer" show={this.state.sources.length > 0} onShow={this.onShow.bind(this)} onHide={this.onHide.bind(this)} backdrop={true} animation={true} size={this.state.expanded ? "fullscreen" : "xl"} aria-labelledby="contained-modal-title-vcenter" centered>
                    <Modal.Header className="flex-row align-items-center me-3" closeVariant='white' closeButton>
                        <Modal.Title id="contained-modal-title-vcenter" style={{flexGrow: 1, flexShrink: 1, flexBasis: 'auto'}}>
                            <EditableText row={1} value={name} onEditing={this.onEditing.bind(this)} onChange={this.rename.bind(this)} />
                        </Modal.Title>
                        <Flag name="expand" className="px-2" tooltip={this.state.expanded ? "Shrink" : "Expand"} value={this.state.expanded} set="bi-arrows-angle-contract" unset="bi-arrows-angle-expand" onChange={this.onToggleExpand.bind(this)} />
                    </Modal.Header>
                    <Modal.Body>
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

const mapStateToProps = (state) => ({
    search: {
        path: state.search.path,
        query: state.search.query,
    }
});

export default connect(mapStateToProps, null, null, { forwardRef: true })(React.forwardRef((props, ref) => (
    <MediaViewer ref={ref} {...props} match={{ params: useParams() }} />
)));
