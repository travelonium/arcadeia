import React from 'react';
import cx from 'classnames';
import Viewer from 'viewerjs';
import { extract } from './../utils';
import 'viewerjs/dist/viewer.css';
import _ from 'lodash';

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
        if (!_.isEqual(this.props.sources, prevProps.sources)) {
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
        // console.log(this);
    }

    sources(items) {
        return items.map((item) => ("/preview/photo/" + item.id + "/" + item.name));
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