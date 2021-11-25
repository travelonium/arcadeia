import React from 'react';
import videojs from 'video.js';

export class VideoPlayer extends React.Component {

    componentDidMount() {
        // instantiate Video.js
        let sources = this.props.sources;
        let options = this.props.options;
        options.sources = sources;
        this.player = videojs(this.videoNode, options, this.onPlayerReady.bind(this));
    }

    // destroy player on unmount
    componentWillUnmount() {
        if (this.player) {
            this.player.dispose()
        }
    }

    onPlayerReady() {
        // console.log(this);
    }

    // wrap the player in a div with a `data-vjs-player` attribute so videojs won't create additional wrapper in the DOM
    // see https://github.com/videojs/video.js/pull/3856
    render() {
        return (
            <div>
                <div data-vjs-player>
                    <video ref={node => this.videoNode = node} className="video-js vjs-big-play-centered"></video>
                </div>
            </div>
        )
    }
}