import React from 'react';
import cx from 'classnames';
import Viewer from 'viewerjs';
import { extract } from './../utils';
import 'viewerjs/dist/viewer.css';

export class PhotoViewer extends React.Component {

    componentDidMount() {
        // instantiate Viewer.js
        this.viewer = new Viewer(this.imagesElement, Object.assign({}, this.props.options, {
            viewed: this.onViewed.bind(this)
        }))
    }

    // destroy viewer on unmount
    componentWillUnmount() {
        if (this.viewer) {
            this.viewer.destroy()
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
                sources.map(source => {
                    return (
                        <img id="image" src={source} />
                    );
                })
            }
            </div>
        )
    }
}