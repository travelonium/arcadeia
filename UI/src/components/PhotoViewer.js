import React from 'react';
import cx from 'classnames';
import Viewer from 'viewerjs';
import { extract } from './../utils';
import 'viewerjs/dist/viewer.css';

export class PhotoViewer extends React.Component {

    componentDidMount() {
        // instantiate Viewer.js
        this.viewer = new Viewer(this.imageElement, Object.assign({}, this.props.options, {
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
        const source = extract("", this.props.sources, 0);
        return (
            <div className={cx(this.props.className, "photo-viewer")}>
                <img id="image" ref={element => this.imageElement = element} src={source} />
            </div>
        )
    }
}