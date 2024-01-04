import React from 'react';
import videojs from 'video.js';
import vttThumbnails from 'videojs-vtt-thumbnails';
import { extract } from '../utils';
import cx from 'classnames';
import _ from 'lodash';

export class VideoPlayer extends React.Component {

    componentDidMount() {
        // instantiate Video.js
        let options = this.props.options;
        let sources = this.sources(this.props.sources);
        options.sources = sources;
        this.player = videojs(this.videoElement, options, this.onPlayerReady.bind(this));
        if (this.props.sources.length === 1) {
            let source = this.props.sources[0];
            this.player.vttThumbnails = vttThumbnails;
            this.player.vttThumbnails({
                src: window.location.origin + "/thumbnails/" + source.id + "/sprite.vtt",
                showTimestamp: false,
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
        if (!_.isEqual(this.props.sources, prevProps.sources)) {
            // a reload becomes necessary only when the fullPath of one or more sources have changed
            let reload = prevProps.sources.reduce((previousValue, currentValue, currentIndex) => {
                if (currentIndex >= this.props.sources.length) return true;
                return (previousValue | (extract(null, currentValue, "fullPath") !== extract(null, this.props.sources, currentIndex, "fullPath")))
            }, false)
            if (reload) {
                let sources = this.sources(this.props.sources);
                this.player.src(sources);
            }
        }
    }

    onPlayerReady() {
        // console.log(this);
    }

    sources(items) {
        return items.map((item) => {
            switch (item.extension) {
                case "wmv":
                case "flv":
                case "mov":
                case "avi":
                case "asf":
                case "vob":
                case "mkv":
                    return "/preview/stream/" + item.id + "/stream.m3u8";
                default:
                    return "/preview/video/" + item.id + "/" + item.name;
            }
        });
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