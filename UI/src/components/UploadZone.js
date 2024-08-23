import React, { Component } from 'react';
import { toast } from 'react-toastify';
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

    isValidHttpUrl(string) {
        let url;
        try {
            url = new URL(string);
        } catch (_) {
            return false;
        }
        return url.protocol === "http:" || url.protocol === "https:";
    }

    renderErrorToast(title, subtitle) {
        return (
            <>
                <div>
                    <strong>{title}</strong>
                </div>
                <small>{subtitle}</small>
            </>
        );
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
            for (const type of event.dataTransfer.types) {
                if ((type === "Files") || (type.match('^text/plain'))) {
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
            const dataTransfer = event.dataTransfer;
            for (const item of dataTransfer.items) {
                if (item.kind === 'string' && item.type.match('^text/plain')) {
                    // If the item is the URL of the image
                    item.getAsString(async (url) => {
                        if (!this.isValidHttpUrl(url)) return;
                        try {
                            const response = await fetch(url);
                            const blob = await response.blob();
                            // MIME to extension mapping for images and videos
                            const mimeExtension = {
                                'image/jpeg': '.jpg',
                                'image/png': '.png',
                                'image/gif': '.gif',
                                'image/bmp': '.bmp',
                                'image/svg+xml': '.svg',
                                'image/webp': '.webp',
                                'image/tiff': '.tiff',
                                'image/heic': '.heic',
                                'video/mp4': '.mp4',
                                'video/mpeg': '.mpeg',
                                'video/quicktime': '.mov',
                                'video/webm': '.webm',
                                'video/ogg': '.ogv',
                                'video/x-msvideo': '.avi',
                                'video/x-ms-wmv': '.wmv',
                                'video/3gpp': '.3gp',
                                'video/3gpp2': '.3g2',
                                'video/x-matroska': '.mkv'
                            };
                            // Attempt to extract a filename from the URL
                            const urlComponents = new URL(url);
                            let filename = urlComponents.pathname.split('/').pop();
                            // Check if URL derived filename seems valid
                            if (!filename.includes('.')) {
                                // If no extension in filename, append one based on MIME type
                                const defaultExt = mimeExtension[blob.type] || '.bin';
                                filename += defaultExt;
                            }
                            let file = new File([blob], filename, { type: blob.type });
                            if (this.props.onUpload !== undefined) this.props.onUpload([file]);
                        } catch (error) {
                            let title = "Fetch Error";
                            console.error("Error fetching or processing the dropped link:", error.message);
                            toast.error(this.renderErrorToast(title,  error.message));
                        }
                    });
                }
            }
            let items = Array.from(dataTransfer.items || []);
            if (this.props.onUpload !== undefined) this.props.onUpload(items);
        });
    }

    render() {
        return (
            <form id="upload-zone-form" className="d-flex" onDragEnter={this.onDrag.bind(this)} onDragOver={this.onDrag.bind(this)} onDragExit={this.onDrag.bind(this)} onDrop={this.onDrop.bind(this)} onSubmit={(event) => event.preventDefault()}>
                <input type="file" id="upload-zone-input" className="d-none" name="files" webkitdirectory="true" multiple={true} onChange={this.onChange.bind(this)} />
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
