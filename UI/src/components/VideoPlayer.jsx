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
import videojs from 'video.js';
import { isEqual } from 'lodash';
import { extract } from '../utils';
import "jb-videojs-hls-quality-selector";
import '../plugins/video.js/videojs-vtt-thumbnails';

export class VideoPlayer extends React.Component {

    constructor(props) {
        super(props);
        this.player = null;
        this.videoJsContainer = React.createRef();
    }

    componentDidMount() {
        this.attempt = 0;
        if (!this.player) {
            const options = this.props.options;
            const sources = this.sources(this.props.sources);
            options.sources = sources;
            const videoElement = document.createElement('video-js');
            videoElement.classList.add('vjs-big-play-centered');
            if (this.videoJsContainer.current) {
                this.videoJsContainer.current.appendChild(videoElement);
                this.player = videojs(videoElement, options, this.onPlayerReady.bind(this));
                this.player.on('loadstart', this.onPlayerLoadStart.bind(this));
                this.player.on('loadeddata', this.onPlayerLoadedData.bind(this));
                this.player.on('error', this.onPlayerError.bind(this));
                if (this.props.sources.length === 1) {
                    let source = this.props.sources[0];
                    this.player.vttThumbnails({
                        src: window.location.origin + "/api/thumbnails/" + source.id + "/sprite.vtt",
                        showTimestamp: false,
                    });
                    this.player.hlsQualitySelector({
                        vjsIconClass: "vjs-icon-cog",
                        displayCurrentQuality: false,
                    });
                }
            }
        }
    }

    // destroy player on unmount
    componentWillUnmount() {
        if (this.player && !this.player.isDisposed()) {
            this.player.dispose();
            this.player = null;
        }
    }

    componentDidUpdate(prevProps) {
        if (!isEqual(this.props.sources, prevProps.sources)) {
            // a reload becomes necessary only when the fullPath of one or more sources have changed
            let reload = prevProps.sources.reduce((previousValue, currentValue, currentIndex) => {
                if (currentIndex >= this.props.sources.length) return true;
                return (previousValue | (extract(null, currentValue, "fullPath") !== extract(null, this.props.sources, currentIndex, "fullPath")))
            }, false)
            if (reload) this.reload();
        }
    }

    onPlayerReady() { }

    onPlayerLoadStart() { }

    onPlayerLoadedData() { }

    onPlayerError() {
        let error = this.player.error();
        // check if the error code is 4 (MEDIA_ERR_SRC_NOT_SUPPORTED)
        if (error && error.code === MediaError.MEDIA_ERR_SRC_NOT_SUPPORTED) {
            this.player.error(null); // clear the error state
            this.reload(true); // try to reload the player forcing format conversion
        }
    }

    sources(items, force = false) {
        this.attempt++;
        // a maximum of two attempts are enough to give up
        if (this.attempt > 2) return null;
        return items.map((item) => {
            if (force) return "/api/preview/video/" + item.id + "/original.m3u8";
            switch (item.extension) {
                case "wmv":
                case "flv":
                case "mov":
                case "avi":
                case "asf":
                case "vob":
                case "mkv":
                    return "/api/preview/video/" + item.id + "/original.m3u8"; // To enable quality selection: "/master.m3u8"
                default:
                    return "/api/preview/video/" + item.id + "/" + item.name;
            }
        });
    }

    reload(force = false) {
        let sources = this.sources(this.props.sources, force);
        this.player.autoplay(this.props.options.autoplay);
        if (sources) this.player.src(sources);
    }

    // wrap the player in a div with a `data-vjs-player` attribute so videojs won't create additional wrapper in the DOM
    // see https://github.com/videojs/video.js/pull/3856
    render() {
        return (
            <div ref={this.videoJsContainer} className={cx(this.props.className, "video-player")} data-vjs-player />
        )
    }
}