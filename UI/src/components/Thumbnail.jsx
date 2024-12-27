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
import { extract } from '../utils';
import cx from 'classnames';

export class Thumbnail extends Component {

    static displayName = Thumbnail.name;

    constructor(props) {
        super(props);
        this.animateInterval = null;
        const thumbnails = extract(0, this.props, 'source', 'thumbnails');
        this.state = {
            index: -1,
            loaded: true,
            id: extract(null, this.props, 'source', 'id'),
            type: extract(null, this.props, 'source', 'type'),
            count: (this.props.animated === false) ? Math.min(thumbnails, 1) : thumbnails,
            children: extract([], this.props, 'source', 'children').reverse().reduce((previous, item) => {
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
        return "/api/thumbnails/" + id + "/" + (this.props.size ? this.props.size : index) + ".jpg";
    }

    animate() {
        let index = this.state.index;
        let viewing = extract(false, this.props, "library", "current", "viewing");
        if (((!viewing) || (index === -1)) && (this.state.count > 0) && (this.state.loaded)) {
            this.setState({
                index: (index < (this.state.count - 1)) ? ++index : 0,
                loaded: false,
            });
        }
    }

    render() {
        return (
            <div className={cx("thumbnail", this.props.className, (this.state.children.length > 0) ? "" : "childless", "d-flex")}>
                <div className="thumbnail-icon-wrapper align-self-center text-center position-absolute w-100">
                    { ((this.state.type === "Folder") && (this.state.children.length === 0)) ? <i className="thumbnail-icon bi bi-folder-fill"></i> : <></> }
                </div>
                {
                    ((this.state.type === "Folder") && (this.state.children.length > 0)) ? this.state.children.map((id, index) => <Card.Img key={index} src={this.thumbnail(id, 0)} />)
                    : <Card.Img src={this.thumbnail(this.state.id, this.state.index)} onLoad={() => this.setState({ loaded: true })} />
                }
                { (this.state.count > 1) ? <ProgressBar variant="info" min={1} max={(this.state.id != null) ? this.state.count : 0} now={this.state.index + 1} className={((this.state.id != null) && (this.state.count)) ? "visible" : "invisible"} /> : <></> }
            </div>
        );
    }
}
