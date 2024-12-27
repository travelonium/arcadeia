/*
 *  Copyright © 2024 Travelonium AB
 *
 *  This file is part of Arcadeia.
 *
 *  Arcadeia is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Affero General Public License as published
 *  by the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Arcadeia is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU Affero General Public License for more details.
 *
 *  You should have received a copy of the GNU Affero General Public License
 *  along with Arcadeia. If not, see <https://www.gnu.org/licenses/>.
 *
 */

import update from 'immutability-helper';
import React, { Component } from 'react';
import Modal from 'react-bootstrap/Modal';
import { Flag } from './toolbar/Flag';
import { connect } from "react-redux";
import { VideoPlayer } from './VideoPlayer';
import { PhotoViewer } from './PhotoViewer';
import { EditableText } from './EditableText';
import { extract, clone, withRouter } from '../utils';

class MediaViewer extends Component {

    static displayName = MediaViewer.name;

    constructor(props) {
        super(props);
        this.videoPlayer = React.createRef();
        this.photoViewer = React.createRef();
        this.state = {
            index: -1,
            sources: [],
            expanded: false,
            videoJsOptions: {
                inactivityTimeout: 5000,
                aspectRatio: "16:9",
                responsive: true,
                autoplay: true,
                controls: true,
                controlBar: {
                    skipButtons: {
                        backward:30,
                        forward: 30
                    }
                },
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

    onShow() {
        const expanded = this.state.expanded;
        const source = extract(null, this.state.sources, this.state.index);
        if (source.type === "Photo") {
            if (expanded) this.photoViewer.current.viewer.full();
            else this.photoViewer.current.viewer.exit();
        } else {
            if (expanded) this.videoPlayer.current.player.fill(true);
            else this.videoPlayer.current.player.aspectRatio(this.state.videoJsOptions.aspectRatio);
        }
    }

    onHide() {
        this.setState({
            sources: [],
        }, () => {
            if (this.props.onHide !== undefined) this.props.onHide();
        });
    }

    onToggleFavorite(value, event) {
        this.toggle("Favorite");
    }

    onToggleExpand(value, event) {
        const source = extract(null, this.state.sources, this.state.index);
        if (source.type === "Photo") {
            if (value) this.photoViewer.current.viewer.full();
            else this.photoViewer.current.viewer.exit();
        } else {
            this.setState({
                expanded: value
            }, () => {
                if (value) this.videoPlayer.current.player.fill(true);
                else this.videoPlayer.current.player.aspectRatio(this.state.videoJsOptions.aspectRatio);
            });
        }
    }

    onEditing(editing) {
        this.props.library.current.editing = editing;
    }


    onDrag(event) {
        this.props.uploadZone.current.onDrag(event);
    }

    onDrop(event) {
        this.props.uploadZone.current.onDrop(event);
    }

    view(sources, index) {
        let source = sources[index];
        if ((source.type !== "Photo") && (source.type !== "Video")) return;
        this.setState({
            index: index,
            sources: sources,
        }, () => {
            if (this.props.onShow !== undefined) {
                this.props.onShow();
            }
        });
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
                <Modal className="media-viewer" show={this.state.sources.length > 0} onShow={this.onShow.bind(this)} onHide={this.onHide.bind(this)}  onDragEnter={this.onDrag.bind(this)} onDragOver={this.onDrag.bind(this)} onDragExit={this.onDrag.bind(this)} onDrop={this.onDrop.bind(this)} backdrop={true} animation={true} size={this.state.expanded ? "fullscreen" : "xl"} aria-labelledby="contained-modal-title-vcenter" centered>
                    <Modal.Header className="flex-row align-items-center me-3" closeButton>
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
        query: state.search.query,
    }
});

export default connect(mapStateToProps, null, null, { forwardRef: true })(
    withRouter(MediaViewer)
);
