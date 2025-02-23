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

import Card from 'react-bootstrap/Card';
import React, { Component } from 'react';
import ProgressBar from 'react-bootstrap/ProgressBar';
import cx from 'classnames';

export class Thumbnail extends Component {

    static displayName = Thumbnail.name;

    constructor(props) {
        super(props);
        this.animateInterval = null;
        const thumbnails = this.props?.source?.thumbnails ?? 0;
        this.state = {
            index: -1,
            loaded: true,
            id: this.props.source?.id,
            type: this.props.source?.type,
            count: (this.props.animated === false) ? Math.min(thumbnails, 1) : thumbnails
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

    icon(type, count, children) {
        if (type === "Folder" && children.length > 0) return null;
        if ((type === "Video" || type === "Photo" || type === "Audio") && count > 0) return null;
        switch (type) {
            case "Folder":
                return <i className="thumbnail-icon bi bi-folder-fill" />;
            case "Video":
                return <i className="thumbnail-icon bi bi-play-btn-fill" />;
            case "Photo":
                return <i className="thumbnail-icon bi bi-image-fill" />;
            case "Audio":
                return <i className="thumbnail-icon bi bi-cassette-fill" />;
            default:
                return null;
        }
    }

    src(id, index, size) {
        // check whether the item has any thumbnails at all and if not, display a broken thumbnail
        if ((index < 0) || (id === null)) return "/placeholder.png";
        return "/api/thumbnails/" + id + "/" + (size ? size : index) + ".jpg";
    }

    thumbnail(type, id, index, children, size) {
        if (type === "Folder" && children.length > 0) {
            return children.map((id, index) => <Card.Img key={index} src={this.src(id, 0, size)} />);
        } else {
            return <Card.Img src={this.src(id, index, size)} onLoad={() => this.setState({ loaded: true })} />;
        }
    }

    animate() {
        let index = this.state.index;
        let viewing = this.props.library?.current?.viewing ?? false;
        if (((!viewing) || (index === -1)) && (this.state.count > 0) && (this.state.loaded)) {
            this.setState({
                index: (index < (this.state.count - 1)) ? ++index : 0,
                loaded: false,
            });
        }
    }

    render() {
        return (
            <div className={cx("thumbnail", this.props.className, "d-flex position-relative")}>
                <div className="thumbnail-icon-wrapper align-self-center text-center position-absolute">
                {
                    this.icon(this.state.type, this.state.count, this.props.children?.length) ?? <></>
                }
                </div>
            {
                this.thumbnail(this.state.type, this.state.id, this.state.index, this.props.children ?? [], this.props.size)
            }
            {
                (this.state.count > 1) ?
                <ProgressBar variant="info" min={1} max={(this.state.id != null) ? this.state.count : 0} now={this.state.index + 1} className={((this.state.id != null) && (this.state.count)) ? "visible" : "invisible"} />
                : <></>
            }
            </div>
        );
    }
}
