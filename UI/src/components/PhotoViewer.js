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
            this.viewer.destroy()
        }
    }

    componentDidUpdate(prevProps) {
        let sources = this.props.sources;
        if (!_.isEqual(sources, prevProps.sources)) {
            this.viewer.update();
        }
    }

    onViewed() {
        // console.log(this);
    }

    render() {
        const sources = extract([], this.props, 'sources');
        return (
            <div ref={element => this.imagesElement = element} className={cx(this.props.className, "photo-viewer")}>
            {
                sources.map((source, index) => {
                    const description = extract("", source, 'description');
                    return (
                        <img key={index} id="image" src={source} alt={description} />
                    );
                })
            }
            </div>
        )
    }
}