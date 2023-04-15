import React, { Component } from 'react';
import cx from 'classnames';
import './UploadZone.scss';

export class UploadZone extends Component {

    static displayName = UploadZone.name;

    constructor(props) {
        super(props);
        this.state = {
            dragging: false,
        };
    }

    onChange(event) {
        event.preventDefault();
    }

    onDrag(event) {
        event.preventDefault();
        event.stopPropagation();
        let dragging = this.state.dragging;
        // are file(s) being dragged or else?
        if (event.dataTransfer.types) {
            for (var i=0; i<event.dataTransfer.types.length; i++) {
                if (event.dataTransfer.types[i] === "Files") {
                    dragging = true;
                }
            }
        }
        if (event.type === "dragenter" || event.type === "dragover") dragging &= true;
        else if (event.type === "dragleave" || event.type === "dragexit") dragging = false;
        this.setState({
            dragging: dragging
        });
    }

    onDrop(event) {
        event.preventDefault();
        event.stopPropagation();
        this.setState({
            dragging: false
        }, () => {
            let files = Array.from(event.dataTransfer.files);
            if (this.props.onUpload !== undefined) this.props.onUpload(files);
        });
    }

    render() {
        return (
            <form id="upload-zone-form" className="d-flex" onDragEnter={this.onDrag.bind(this)} onDragOver={this.onDrag.bind(this)} onDragExit={this.onDrag.bind(this)} onDrop={this.onDrop.bind(this)} onSubmit={(event) => event.preventDefault()}>
                <input type="file" id="upload-zone-input" className="d-none" name="files" multiple={true} onChange={this.onChange.bind(this)} />
                <label id="upload-zone-label" htmlFor="upload-zone-input" className="d-flex" onClick={(event) => event.preventDefault()}>
                    <div className={cx("upload-zone-overlay justify-content-center align-items-center", this.state.dragging ? "dragging" : "d-none")}>
                        <p className="font-weight-light h5 text-uppercase">Drop Here To Upload</p>
                    </div>
                    <div className={cx("upload-zone-content", this.state.dragging ? "dragging" : "")}>
                        {this.props.children}
                    </div>
                </label>
            </form>
        );
    }
}
