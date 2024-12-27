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

import React from 'react';
import cx from 'classnames';
import Viewer from 'viewerjs';
import { isEqual } from 'lodash';
import { extract } from '../utils';
import 'viewerjs/dist/viewer.css';


export class PhotoViewer extends React.Component {

    componentDidMount() {
        // instantiate Viewer.js
        this.viewer = new Viewer(this.imagesElement, Object.assign({}, this.props.options, {
            viewed: this.onViewed.bind(this)
        }));
    }

    // destroy viewer on unmount
    componentWillUnmount() {
        if (this.viewer) {
            this.viewer.destroy();
        }
    }

    componentDidUpdate(prevProps) {
        if (!isEqual(this.props.sources, prevProps.sources)) {
            // a reload becomes necessary only when the fullPath of one or more sources have changed
            let reload = prevProps.sources.reduce((previousValue, currentValue, currentIndex) => {
                if (currentIndex >= this.props.sources.length) return true;
                return (previousValue | (extract(null, currentValue, "fullPath") !== extract(null, this.props.sources, currentIndex, "fullPath")))
            }, false)
            if (reload) {
                this.viewer.update();
            }
        }
    }

    onViewed() {
    }

    sources(items) {
        return items.map((item) => ("/api/preview/photo/" + item.id + "/" + item.name));
    }

    render() {
        const sources = this.sources(this.props.sources);
        return (
            <div ref={element => this.imagesElement = element} className={cx(this.props.className, "photo-viewer")}>
            {
                sources.map((source, index) => {
                    const description = extract("", source, "description");
                    return (
                        <img key={index} id="image" src={source} alt={description} />
                    );
                })
            }
            </div>
        )
    }
}