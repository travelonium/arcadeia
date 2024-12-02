import Badge from 'react-bootstrap/Badge';
import React, { Component } from 'react';
import { toast } from 'react-toastify';
import * as pb from 'path-browserify';
import cx from 'classnames';

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
            const items = [];
            const dataTransfer = event.dataTransfer;
            for (const item of dataTransfer.items) {
                if (item.kind === 'string' && item.type.match('^text/plain')) {
                    // the item is a URL
                    item.getAsString(async (url) => {
                        const items = []; // only valid and processed inside this block
                        if (!this.isValidHttpUrl(url)) return;
                        try {
                            const response = await fetch(url);
                            const blob = await response.blob();
                            // MIME to extension mapping for images and videos
                            const extensions = {
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
                            // attempt to extract a filename from the URL
                            const components = new URL(url);
                            let filename = pb.basename(components.pathname).toLowerCase();
                            let extension = pb.extname(components.pathname).toLowerCase();
                            // is the downloaded file an html file?
                            if (blob.type.match('^text/html')) {
                                items.push(url);
                            } else {
                                // nope, not an html file
                                if (extension && Object.values(extensions).includes(extension)) {
                                    // the filename includes an extension and it is supported
                                    const file = new File([blob], filename, { type: blob.type });
                                    // pass it on to the uploader as a file
                                    items.push(file);
                                } else if ((extension = extensions[blob.type])) {
                                    // no extension in filename, but the MIME type is supported, infer the extension
                                    filename += extension;
                                    const file = new File([blob], filename, { type: blob.type });
                                    // pass it on to the uploader as a file
                                    items.push(file);
                                } else {
                                    // nope, it's an unsupported file type, ignore it
                                    const title = "Unsupported Media Type";
                                    const message = "Media file type is unsupported.";
                                    console.error(title, message);
                                    toast.error(this.renderErrorToast(title, message));
                                }
                            }
                        } catch (error) {
                            console.warn(error.message);
                            // toast.error(this.renderErrorToast("Fetch Error",  error.message));
                            // as a last resort, pass on the URL to the uploader
                            items.push(url);
                        }
                        // process the list of items to be uploaded
                        if (items && this.props.onUpload !== undefined) this.props.onUpload(items);
                    });
                } else {
                    items.push(item);
                }
            }
            if (items && this.props.onUpload !== undefined) this.props.onUpload(items);
        });
    }

    render() {
        return (
            <form id="upload-zone-form"
                className={cx("upload-zone d-flex position-absolute", this.props.className)}
                style={{ width: this.props?.width }}
                onSubmit={(event) => event.preventDefault()}
                onDragEnter={this.onDrag.bind(this)}
                onDragOver={this.onDrag.bind(this)}
                onDragExit={this.onDrag.bind(this)}
                onDrop={this.onDrop.bind(this)}
            >
                <input type="file" id="upload-zone-input" className="d-none" name="files" webkitdirectory="true" multiple={true} onChange={this.onChange.bind(this)} />
                <label id="upload-zone-label" htmlFor="upload-zone-input" className="d-flex" onClick={(event) => event.preventDefault()}>
                    <div className={cx("upload-zone-overlay justify-content-center align-items-center", this.state.dragging ? "dragging" : "d-none")}>
                        <Badge className="d-flex align-items-center px-4" bg="light" pill>
                        <p className="d-flex fw-lighter text-dark text-uppercase h5 m-1">
                            Drop To Upload
                        </p>
                        </Badge>
                    </div>
                    <div className={cx("upload-zone-content", this.state.dragging ? "dragging" : "")}>
                        {this.props.children}
                    </div>
                </label>
            </form>
        );
    }
}
