import React from 'react';
import videojs from 'video.js';
import cx from 'classnames';
import _ from 'lodash';

export class VideoPlayer extends React.Component {

    componentDidMount() {
        // instantiate Video.js
        let sources = this.props.sources;
        let options = this.props.options;
        options.sources = sources;
        this.player = videojs(this.videoElement, options, this.onPlayerReady.bind(this));
    }

    // destroy player on unmount
    componentWillUnmount() {
        if (this.player) {
            this.player.dispose()
        }
    }

    componentDidUpdate(prevProps) {
        let sources = this.props.sources;
        if (!_.isEqual(sources, prevProps.sources)) {
            this.player.src(sources);
        }
    }

    onPlayerReady() {
        // console.log(this);
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