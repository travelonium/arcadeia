import React from 'react';
import videojs from 'video.js';
import vttThumbnails from 'videojs-vtt-thumbnails';
import "jb-videojs-hls-quality-selector";
import { extract } from '../utils';
import { isEqual } from 'lodash';
import cx from 'classnames';

export class VideoPlayer extends React.Component {

    componentDidMount() {
        this.attempt = 0;
        const options = this.props.options;
        const sources = this.sources(this.props.sources);
        options.sources = sources;
        videojs.registerPlugin("vttThumbnails", vttThumbnails);
        this.player = videojs(this.videoElement, options, this.onPlayerReady.bind(this));
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

    // destroy player on unmount
    componentWillUnmount() {
        if (this.player) {
            this.player.dispose();
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

    onPlayerReady() {}

    onPlayerLoadStart() {}

    onPlayerLoadedData() {}

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
        if (sources ) this.player.src(sources);
    }

    // wrap the player in a div with a `data-vjs-player` attribute so videojs won't create additional wrapper in the DOM
    // see https://github.com/videojs/video.js/pull/3856
    render() {
        return (
            <div className={cx(this.props.className, "video-player")}>
                <div data-vjs-player>
                    <video ref={element => this.videoElement = element} className="video-js vjs-big-play-centered"></video>
                </div>
            </div>
        )
    }
}