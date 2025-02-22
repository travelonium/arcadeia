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

import axios from 'axios';
import cx from 'classnames';
import pb from 'path-browserify';
import { connect } from "react-redux";
import { toast } from 'react-toastify';
import React, { Component } from 'react';
import Badge from 'react-bootstrap/Badge';
import ProgressToast from './ProgressToast';
import { extract, withRouter } from '../utils';
import { Container, Row, Col } from 'react-bootstrap';
import { queueUpload, startUploadThunk, updateUpload, switchUploadState } from '../features/ui/slice';
import { selectAll, selectActive, selectQueued, selectSucceeded, selectFailed } from '../features/ui/selectors';

class UploadZone extends Component {

    static displayName = UploadZone.name;

    constructor(props) {
        super(props);
        this.uploadTimeout = null;
        this.state = {
            dragging: false,
            timestamp: Object.values(props.ui.uploads.items).reduce((oldest, item) => {
                return (item.state === 'queued' && item.timestamp < oldest) ? item.timestamp : oldest;
            }, Date.now())
        };
    }

    get path() {
        const pathname = this.props?.location?.pathname;
        try {
            const decodedPath = decodeURI(pathname || "");
            const components = decodedPath.match(/(.*\/)(.*)?/);
            if (components) return components[1];
            return null;
        } catch (error) {
            console.error('Error decoding the URI:', error);
            return null;
        }
    }

    get all() {
        return this.props.ui.uploads.all.filter(item => item.timestamp >= this.state.timestamp);
    }

    get queued() {
        return this.props.ui.uploads.queued.filter(item => item.timestamp >= this.state.timestamp && item.state === 'queued');
    }

    get active() {
        return this.props.ui.uploads.active.filter(item => item.timestamp >= this.state.timestamp && item.state === 'active');
    }

    get failed() {
        return this.props.ui.uploads.failed.filter(item => item.timestamp >= this.state.timestamp && item.state === 'failed');
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

    upload(items = null, defer = false) {
        if (defer) {
            if (this.uploadTimeout !== null) clearTimeout(this.uploadTimeout);
            this.uploadTimeout = setTimeout(() => this.upload(), 100);
            return;
        }
        if (items) {
            let queue = (item, path) => {
                if (typeof item === 'string') {
                    this.props.dispatch(queueUpload({
                        key: path + item,
                        value: {
                            url: item,
                            path: path
                        }
                    }));
                } else {
                    this.props.dispatch(queueUpload({
                        key: pb.join(path, item.name),
                        value: {
                            name: item.name,
                            file: item,
                            path: path,
                        }
                    }));
                }
                this.upload(null, true);
            }
            let process = (item, path) => {
                if (typeof item !== 'string') {
                    // the item is a file object, normalize its path before queueing it
                    if (item.webkitRelativePath) path = pb.normalize(pb.dirname(pb.join(path, item.webkitRelativePath)) + "/");
                }
                queue(item, path);
            };
            let traverse = (item, path = this.path) => {
                if (item.isFile) {
                    item.file(file => process(file, path));
                } else if (item.isDirectory) {
                    let dirReader = item.createReader();
                    dirReader.readEntries(entries => {
                        entries.forEach(entry => {
                            traverse(entry);
                        });
                    });
                }
            };
            items.forEach(item => {
                if (item.webkitGetAsEntry) item = item.webkitGetAsEntry();
                if (item === null) return;
                if (typeof item === 'string') {
                    process(item, this.path);
                } else if (item.isDirectory) {
                    traverse(item);
                } else {
                    if (item instanceof File) process(item, this.path);
                    else item.file(file => process(file, this.path));
                }
            });
            return;
        }
        // do we already have the maximum simultaneous number of active uploads?
        const active = this.active.length;
        if (active >= this.props.ui.uploads.simultaneous) return;
        // nope, do we have any items in the queue? if not, log the list of failed uploads if any
        const queued = this.queued.length;
        if (queued === 0) {
            // if no uploads are currently active either, reset the timestamp
            if (active === 0) {
                const failed = this.failed;
                if (failed.length > 0) console.warn("Failed Uploads:", failed);
                this.setState({
                    timestamp: Date.now()
                });
            }
            return;
        }
        // yes, we can start one more
        // dequeue the oldest queued item and start uploading it
        this.props.dispatch(startUploadThunk()).then(({ key, item }) => {
            if (key) {
                const url = item?.url;
                const file = item?.file;
                const path = item?.path;
                if (file) {
                    this.uploadFile(file, key, path);
                } else if (url) {
                    this.uploadUrl(url, key, path);
                } else {
                    console.error("Neither the file nor the URL were valid!");
                }
            } else {
                console.error("Failed to start an upload!");
            }
        });
    }

    uploadFile(file, key, path) {
        const fileName = file.name;
        const total = this.all.length;
        const index = total - this.queued.length;
        let data = new FormData();
        data.append('files', file, pb.join(path, file.name));
        const prefix = "[" + index + " / " + total + "] ";
        const toastId = toast.info(<ProgressToast title={prefix + "Uploading..."} subtitle={file.name} />,
            {
                progress: 0,
                autoClose: false,
                theme: (this.props.ui.theme === 'dark') ? 'dark' : 'light',
                icon: <div className="Toastify__spinner"></div>
            }
        );
        this.props.dispatch(updateUpload({
            key: key,
            value: {
                name: file.name,
                path: path,
                toast: toastId,
            }
        }));
        let subtitle = file.name;
        const theme = (this.props.ui.theme === 'dark') ? 'dark' : 'light';
        let config = {
            headers: {
                'Content-Type': 'multipart/form-data',
            },
            validateStatus: (status) => {
                return (status === 200);
            },
            onUploadProgress: this.onUploadFileProgress.bind(this, key, index, "Uploading...", subtitle, undefined, theme, undefined),
        };
        const onShowUploadProgress = (item, fullPath, progress) => {
            if (item === key) {
                this.onUploadFileProgress(key, index, "Processing...", pb.basename(fullPath), undefined, theme, undefined, progress)
            };
        };
        this.props.signalRConnection?.on("ShowUploadProgress", onShowUploadProgress.bind(this));
        const target = new URL("/api/library" + path, window.location.origin);
        target.searchParams.set('overwrite', this.props.ui.uploads.overwrite);
        target.searchParams.set('duplicate', this.props.ui.uploads.duplicate);
        axios.post(target.toString(), data, config)
        .then((response) => {
            subtitle = extract(file.name, response, 'data', 0, 'name');
            this.onUploadFileProgress(key, index, "Upload Complete", subtitle, 'success', null, null, 1.0);
            // set the successfully uploaded item's state to 'succeeded'
            this.props.dispatch(switchUploadState({ key: key, to: 'succeeded' }));
            // reload the grid items if the uploaded file's path is the current path or is in a subdirectory of the current path
            if ((path === this.path) || pb.normalize(pb.dirname(path) + "/") === this.path) {
                if (this.props.onUploadComplete !== undefined) this.props.onUploadComplete(fileName);
            }
            // next!
            this.upload(null, true);
        })
        .catch(error => {
            console.error(error);
            let message = "Upload Failed";
            if (error.response) {
                // the client was given an error response (5xx, 4xx)
                let title = extract(null, error.response, 'data', 'title');
                if (title) message += " (" + title + ")";
            } else if (error.request) {
                // the client never received a response, and the request was never left
            } else {
                // well, something else must've happened
            }
            this.onUploadFileProgress(key, null, message, subtitle, 'error', null, null, null);
            // set the failed upload item's state to 'failed'
            this.props.dispatch(switchUploadState({ key: key, to: 'failed' }));
            // process any remaining queued items
            this.upload(null, true);
        })
        .finally(() => {
            this.props.signalRConnection?.off("ShowUploadProgress", onShowUploadProgress);
        });
        // process any remaining queued items
        this.upload(null, true);
    }

    uploadUrl(url, key, path) {
        const total = this.all.length;
        const index = total - this.queued.length;
        const prefix = "[" + index + " / " + total + "] ";
        const toastId = toast.info(<ProgressToast title={prefix + "Resolving..."} subtitle={url} />,
            {
                progress: 0,
                autoClose: false,
                theme: (this.props.ui.theme === 'dark') ? 'dark' : 'light',
                icon: <div className="Toastify__spinner"></div>
            }
        );
        this.props.dispatch(updateUpload({
            key: key,
            value: {
                url: url,
                path: path,
                toast: toastId,
            }
        }));
        let title;
        // eslint-disable-next-line
        let result;
        let fileName;
        let subtitle = url;
        let progress = null;
        let type = undefined;
        let icon = undefined;
        let theme = (this.props.ui.theme === 'dark') ? 'dark' : 'light';
        const target = new URL("/api/library/upload", window.location.origin);
        target.searchParams.set('overwrite', this.props.ui.uploads.overwrite);
        target.searchParams.set('duplicate', this.props.ui.uploads.duplicate);
        target.searchParams.set('path', path);
        target.searchParams.set('url', url);
        fetch(target.toString())
        .then((response) => {
            let show = true;
            const reader = response.body.getReader();
            const process = ({ done, value: chunk }) => {
                if (done) {
                    progress = 1.0;
                    title = "Upload Complete";
                    this.onUploadProgress(key, index, title, subtitle, progress, type, theme, icon);
                    // set the successfully uploaded item's state to 'succeeded'
                    this.props.dispatch(switchUploadState({ key: key, to: 'succeeded' }));
                    // reload the grid items if the uploaded file's path is the current path or is in a subdirectory of the current path
                    if ((path === this.path) || pb.normalize(pb.dirname(path) + "/") === this.path) {
                        if (this.props.onUploadComplete !== undefined) this.props.onUploadComplete(fileName);
                    }
                    // next!
                    this.upload(null, true);
                    return;
                }
                const lines = new TextDecoder('utf-8').decode(chunk).trim().split('\n');
                for (const line of lines) {
                    const regex = /^([\w\s]+):\s(.+)$/;
                    const match = line.trim().match(regex);
                    if (match) {
                        const k = match[1].trim();
                        const v = match[2].trim();
                        if (k === 'Name') {
                            progress = 0;
                            title = "Starting...";
                            subtitle = fileName = v;
                        } else if (k === 'Downloading') {
                            title = "Downloading...";
                            if (!isNaN(v)) {
                                progress = parseFloat(v) !== 1.0 ? parseFloat(v) : 0;
                            }
                        } else if (k === 'Merging') {
                            progress = 0;
                            title = "Merging...";
                            subtitle = fileName = v;
                        } else if (k === 'Moving') {
                            progress = 0;
                            title = "Moving...";
                            subtitle = fileName = v;
                        } else if (k === 'Processing') {
                            title = "Processing...";
                            if (!isNaN(v)) {
                                progress = parseFloat(v);
                            }
                        } else if (k === 'Result') {
                            icon = null;
                            theme = null;
                            progress = 1.0;
                            type =  'success';
                            title = "Complete";
                            result = JSON.parse(v);
                            show = false;
                        } else if (k === 'Error') {
                            // reject the promise to propagate the error
                            return Promise.reject(new Error(v));
                        }
                        // Update the toast
                        if (show) this.onUploadProgress(key, index, title, subtitle, progress, type, theme, icon);
                    }
                };
                return reader.read().then(process);
            };
            return reader.read().then(process);
        })
        .catch(error => {
            console.error(error);
            title = error.message;
            this.onUploadProgress(key, null, title, subtitle, null, 'error', null);
            // set the failed upload item's state to 'failed'
            this.props.dispatch(switchUploadState({ key: key, to: 'failed' }));
            // process any remaining queued items
            this.upload(null, true);
        });
        // process any remaining queued items
        this.upload(null, true);
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
                        if (items) this.upload(items);
                    });
                } else {
                    items.push(item);
                }
            }
            if (items) this.upload(items);
        });
    }

    onUploadFileProgress(key, index, title, subtitle, type, theme, icon, progressEvent) {
        let progress = 0.0;
        if (typeof progressEvent === 'object' && progressEvent !== null) {
            if (progressEvent.progress === 1.0) {
                progress = 0.0;
                title = "Processing...";
            } else {
                progress = progressEvent.progress;
            }
        } else {
            progress = progressEvent;
        }
        this.onUploadProgress(key, index, title, subtitle, progress, type, theme, icon);
    }

    onUploadProgress(key, index, title, subtitle, progress, type, theme, icon) {
        this.props.dispatch(updateUpload({
            key: key,
            value: {
                status: title,
                name: subtitle,
                progress: progress
            }
        }));
        const prefix = index != null ? `[${index} / ${this.all.length}] ` : "";
        const options = {
            autoClose: progress === 0.0 ? false : null,
            render: <ProgressToast title={`${prefix}${title}`} subtitle={subtitle} />,
            progress: progress !== 1.0 ? progress : null,
            ...(icon !== undefined && { icon }),
            ...(type !== undefined && { type }),
            ...(theme !== undefined && { theme }),
        };
        const toastId = extract(null, this.props.ui.uploads.items, key, 'toast');
        toast.update(toastId, options);
    }

    render() {
        return (
            <form id="upload-zone-form"
                className={cx("upload-zone d-flex flex-grow-1", this.props.className)}
                onSubmit={(event) => event.preventDefault()}
                onDragEnter={this.onDrag.bind(this)}
                onDragOver={this.onDrag.bind(this)}
                onDragExit={this.onDrag.bind(this)}
                onDrop={this.onDrop.bind(this)}
            >
                <input type="file" id="upload-zone-input" className="d-none" name="files" webkitdirectory="true" multiple={true} onChange={this.onChange.bind(this)} />
                <label id="upload-zone-label" htmlFor="upload-zone-input" className="d-flex position-relative flex-grow-1" onClick={(event) => event.preventDefault()}>
                    <Container className={cx("upload-zone-overlay justify-content-center align-items-center", this.state.dragging ? "dragging" : "d-none")} fluid>
                        <Badge className="d-flex align-items-center animate__animated animate__fadeIn animate__zoomIn" bg="light">
                            <Row className="align-items-center gx-0 pt-5">
                                <Col className="d-flex align-items-center justify-content-center" xs={12}>
                                    <span className="upload-zone-box d-flex align-items-center justify-content-center">
                                        <svg className="animate__animated animate__fadeOutUp animate__infinite" width="35" height="35">
                                            <use xmlnsXlink="http://www.w3.org/1999/xlink" xlinkHref="#upload-icon-fill"></use>
                                        </svg>
                                    </span>
                                </Col>
                                <Col className="d-flex align-items-center justify-content-center pt-4 pb-4" xs={12}>
                                    <p className="d-flex fw-bold text-dark text-uppercase h5">
                                        UPLOAD ZONE
                                    </p>
                                </Col>
                            </Row>
                        </Badge>
                    </Container>
                    <div className={cx("upload-zone-content d-flex flex-grow-1", this.state.dragging ? "dragging" : "")}>
                        {this.props.children}
                    </div>
                </label>
            </form>
        );
    }
}

const mapStateToProps = (state) => ({
    ui: {
        uploads: {
            ...state.ui.uploads,
            all: selectAll(state),
            queued: selectQueued(state),
            active: selectActive(state),
            succeeded: selectSucceeded(state),
            failed: selectFailed(state)
        },
    }
});

export default connect(mapStateToProps, null, null, { forwardRef: true })(
    withRouter(UploadZone)
);
