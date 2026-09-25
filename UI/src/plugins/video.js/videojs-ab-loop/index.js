import videojs from 'video.js';
import './styles.scss';

const Plugin = videojs.getPlugin('plugin');
const Button = videojs.getComponent('Button');

class ABLoopButton extends Button {
    handleClick() {
        this.player().abLoop().toggle();
    }
}

videojs.registerComponent('ABLoopButton', ABLoopButton);

class VideoLoopButton extends Button {
    handleClick() {
        this.player().abLoop().toggleVideoLoop();
    }
}

videojs.registerComponent('VideoLoopButton', VideoLoopButton);

export class ABLoop extends Plugin {
    constructor(player) {
        super(player);
        this.phase = 'off';
        this.start = null;
        this.end = null;
        this.listeners = [];
        player.ready(() => {
            if (!this.disposed) this.initialize();
        });
    }

    initialize() {
        const controls = this.player.getChild('controlBar');
        this.seekBar = controls?.getChild('progressControl')?.getChild('seekBar');
        if (!this.seekBar) return;
        this.button = controls.addChild('ABLoopButton', {}, Math.max(0, controls.children().length - 1));
        this.button.addClass('vjs-ab-loop-button');
        this.button.el().querySelector('.vjs-icon-placeholder').setAttribute('aria-hidden', 'true');
        this.loopButton = controls.addChild('VideoLoopButton', {}, Math.max(0, controls.children().length - 1));
        this.loopButton.addClass('vjs-video-loop-button');
        this.loopButton.el().querySelector('.vjs-icon-placeholder').setAttribute('aria-hidden', 'true');
        this.overlay = document.createElement('div');
        this.overlay.className = 'vjs-ab-loop-overlay';
        this.overlay.setAttribute('aria-hidden', 'true');
        this.overlay.innerHTML = '<div class="vjs-ab-loop-range"></div><span class="vjs-ab-loop-marker vjs-ab-loop-a">A</span><span class="vjs-ab-loop-marker vjs-ab-loop-b">B</span>';
        this.seekBar.el().appendChild(this.overlay);
        this.status = document.createElement('div');
        this.status.className = 'vjs-ab-loop-status';
        this.status.setAttribute('role', 'status');
        this.player.el().appendChild(this.status);

        // Capture on the progress control, before Video.js starts its normal scrub.
        const progress = controls.getChild('progressControl').el();
        const listen = (type, handler) => {
            progress.addEventListener(type, handler, { capture: true, passive: false });
            this.listeners.push(() => progress.removeEventListener(type, handler, true));
        };
        const stop = event => {
            event.preventDefault();
            event.stopImmediatePropagation();
        };
        ['mousedown', 'touchstart'].forEach(type => listen(type, event => {
            this.selectingGesture = this.isSelecting();
            if (!this.selectingGesture || (event.button !== undefined && event.button !== 0)) return;
            stop(event);
            const point = event.touches?.[0] || event;
            const rect = this.seekBar.el().getBoundingClientRect();
            if (rect.width) this.select((point.clientX - rect.left) / rect.width * this.player.duration());
        }));
        // Let mousemove reach hover previews, including while marking a point.
        ['mouseup', 'touchmove', 'touchend', 'click'].forEach(type => listen(type, event => {
            if (this.selectingGesture) stop(event);
            if (type === 'click' || type === 'touchend') this.selectingGesture = false;
        }));
        listen('keydown', event => {
            if (event.key === 'Escape' && this.phase !== 'off') {
                stop(event);
                this.clear();
            } else if (this.isSelecting() && (event.key === 'Enter' || event.key === ' ')) {
                stop(event);
                this.select(this.player.currentTime());
            }
        });
        this.on(this.player, ['loadstart', 'emptied'], () => this.clear());
        this.on(this.player, ['durationchange', 'loadedmetadata'], () => {
            if (!this.available() || (this.start !== null && this.start >= this.player.duration()) || this.end > this.player.duration()) this.clear();
            this.render();
        });
        this.on(this.player, ['timeupdate', 'seeked', 'play'], () => this.enforce());
        this.on(this.player, 'ended', () => {
            if (this.phase !== 'active') return;
            this.player.currentTime(this.start);
            this.player.play()?.catch(() => {});
        });
        // timeupdate alone can be hundreds of milliseconds apart.
        this.timer = this.player.setInterval(() => {
            if (!this.player.paused()) this.enforce();
        }, 30);
        this.render();
    }

    available() {
        const duration = this.player.duration();
        return Number.isFinite(duration) && duration > 0;
    }

    isSelecting() {
        return this.phase === 'start' || this.phase === 'end';
    }

    toggle() {
        if (!this.available()) return;
        if (this.phase !== 'off') return this.clear();
        this.player.loop(false);
        this.phase = 'start';
        this.render();
        this.seekBar.el().focus();
    }

    toggleVideoLoop() {
        if (!this.available()) return;
        const enabled = !this.player.loop();
        this.player.loop(enabled);
        if (enabled) this.clear();
        this.render();
    }

    select(time) {
        if (!this.isSelecting() || !this.available() || !Number.isFinite(time)) return;
        time = Math.max(0, Math.min(time, this.player.duration()));
        if (this.phase === 'start') {
            this.start = time;
            this.phase = 'end';
        } else {
            if (Math.abs(time - this.start) < 0.1) {
                this.status.textContent = 'Choose a second point at least 0.1 seconds away.';
                return;
            }
            this.end = Math.max(this.start, time);
            this.start = Math.min(this.start, time);
            this.phase = 'active';
            this.player.currentTime(this.start);
        }
        this.render();
    }

    enforce() {
        if (this.phase !== 'active' || this.player.seeking() || this.player.scrubbing()) return;
        const time = this.player.currentTime();
        if (time < this.start || time >= this.end) this.player.currentTime(this.start);
    }

    clear() {
        this.phase = 'off';
        this.start = this.end = null;
        this.render();
    }

    render() {
        if (!this.button) return;
        const selecting = this.isSelecting();
        const active = this.phase === 'active';
        const videoLoop = Boolean(this.player.loop());
        this.loopButton[this.available() ? 'enable' : 'disable']();
        this.loopButton.el().setAttribute('aria-pressed', String(videoLoop));
        this.loopButton.controlText(videoLoop ? 'Disable full-video loop' : 'Loop entire video');
        this.loopButton.el().classList.toggle('vjs-ab-loop-enabled', videoLoop);
        this.button[this.available() ? 'enable' : 'disable']();
        this.button.el().setAttribute('aria-pressed', String(this.phase !== 'off'));
        this.button.controlText(this.phase === 'off' ? 'Select A–B loop on progress bar' : 'Clear A–B loop');
        this.player.el().classList.toggle('vjs-ab-loop-selecting', selecting);
        this.button.el().classList.toggle('vjs-ab-loop-enabled', this.phase !== 'off');
        const messages = {
            off: '',
            start: 'Select A on the progress bar. Keyboard: seek with arrows, then Enter. Esc cancels.',
            end: 'Select B on the progress bar. Press A–B to cancel.',
            active: `Looping ${videojs.time.formatTime(this.start)} – ${videojs.time.formatTime(this.end)}. Press A–B to clear.`,
        };
        this.status.textContent = messages[this.phase];
        this.overlay.hidden = this.start === null;
        const percentage = time => `${time / this.player.duration() * 100}%`;
        this.overlay.querySelector('.vjs-ab-loop-a').style.left = percentage(this.start);
        const b = this.overlay.querySelector('.vjs-ab-loop-b');
        b.hidden = !active;
        b.style.left = percentage(this.end);
        const range = this.overlay.querySelector('.vjs-ab-loop-range');
        range.hidden = !active;
        range.style.left = percentage(this.start);
        range.style.width = percentage(this.end - this.start);
    }

    dispose() {
        this.disposed = true;
        this.listeners.forEach(remove => remove());
        if (this.timer) this.player.clearInterval(this.timer);
        this.overlay?.remove();
        this.status?.remove();
        this.player.removeClass('vjs-ab-loop-selecting');
        [this.button, this.loopButton].forEach(button => {
            if (button && !button.isDisposed()) {
                this.player.getChild('controlBar')?.removeChild(button);
                button.dispose();
            }
        });
        super.dispose();
    }
}

videojs.registerPlugin('abLoop', ABLoop);
