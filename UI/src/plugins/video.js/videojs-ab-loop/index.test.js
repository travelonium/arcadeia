// @vitest-environment jsdom
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import videojs from 'video.js';
import './index';

describe('A–B loop plugin', () => {
    let player, plugin, time, duration, paused;
    beforeEach(async () => {
        vi.spyOn(HTMLMediaElement.prototype, 'load').mockImplementation(() => {});
        vi.spyOn(HTMLMediaElement.prototype, 'pause').mockImplementation(() => {});
        const video = document.createElement('video');
        document.body.appendChild(video);
        player = videojs(video, { controls: true });
        time = 0;
        duration = 100;
        paused = true;
        vi.spyOn(player, 'duration').mockImplementation(() => duration);
        vi.spyOn(player, 'currentTime').mockImplementation(value => {
            if (value !== undefined) time = value;
            return time;
        });
        vi.spyOn(player, 'paused').mockImplementation(() => paused);
        vi.spyOn(player, 'seeking').mockReturnValue(false);
        vi.spyOn(player, 'play').mockResolvedValue();
        plugin = player.abLoop();
        await new Promise(resolve => player.ready(resolve));
        vi.spyOn(plugin.seekBar.el(), 'getBoundingClientRect').mockReturnValue({ left: 10, width: 200 });
    });
    afterEach(() => {
        player.dispose();
        vi.restoreAllMocks();
        document.body.innerHTML = '';
    });
    function clickAt(x) {
        const bar = plugin.seekBar.el();
        for (const type of ['mousedown', 'mouseup', 'click']) {
            bar.dispatchEvent(new MouseEvent(type, { clientX: x, bubbles: true, cancelable: true }));
        }
    }
    it('selects on the existing bar without invoking its normal scrub handler', () => {
        const scrub = vi.fn();
        plugin.seekBar.el().addEventListener('mousedown', scrub);
        plugin.toggle();
        clickAt(50);
        expect(plugin.phase).toBe('end');
        clickAt(130);
        expect([plugin.start, plugin.end, time]).toEqual([20, 60, 20]);
        expect(scrub).not.toHaveBeenCalled();
        expect(plugin.overlay.querySelector('.vjs-ab-loop-range').style.width).toBe('40%');
        expect(paused).toBe(true);
        plugin.toggle();
        clickAt(80);
        expect(scrub).toHaveBeenCalledOnce();
    });
    it('orders reverse selections and rejects a zero-length range', () => {
        plugin.toggle();
        plugin.select(60);
        plugin.select(60);
        expect(plugin.phase).toBe('end');
        plugin.select(20);
        expect([plugin.start, plugin.end]).toEqual([20, 60]);
    });
    it('marks points with touch without starting native scrubbing', () => {
        plugin.toggle();
        for (const clientX of [50, 130]) {
            const event = new Event('touchstart', { bubbles: true, cancelable: true });
            Object.defineProperty(event, 'touches', { value: [{ clientX }] });
            plugin.seekBar.el().dispatchEvent(event);
            expect(event.defaultPrevented).toBe(true);
            plugin.seekBar.el().dispatchEvent(new Event('touchend', { bubbles: true, cancelable: true }));
        }
        expect([plugin.start, plugin.end]).toEqual([20, 60]);
    });
    it('checks playing video between timeupdate events', async () => {
        plugin.toggle(); plugin.select(20); plugin.select(60);
        paused = false;
        time = 61;
        await new Promise(resolve => setTimeout(resolve, 65));
        expect(time).toBe(20);
    });
    it('loops at B and after out-of-range seeks, but allows seeks inside the loop', () => {
        plugin.toggle(); plugin.select(20); plugin.select(60);
        time = 60; player.trigger('timeupdate'); expect(time).toBe(20);
        time = 10; player.trigger('seeked'); expect(time).toBe(20);
        time = 40; player.trigger('seeked'); expect(time).toBe(40);
        player.trigger('ended'); expect(time).toBe(20);
        expect(player.play).toHaveBeenCalledOnce();
    });
    it('supports keyboard marking and cancellation', () => {
        plugin.toggle();
        time = 20;
        plugin.seekBar.el().dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
        time = 60;
        plugin.seekBar.el().dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
        expect(plugin.phase).toBe('active');
        plugin.seekBar.el().dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
        expect(plugin.phase).toBe('off');
    });
    it('resets for new sources and disables unavailable or live durations', () => {
        plugin.toggle(); plugin.select(20); plugin.select(60);
        player.trigger('loadstart');
        expect(plugin.phase).toBe('off');
        expect(plugin.overlay.hidden).toBe(true);
        duration = Infinity; player.trigger('durationchange'); plugin.toggle();
        expect(plugin.phase).toBe('off');
        expect(plugin.button.el().disabled).toBe(true);
    });
    it('removes its controls and capture listeners when disposed separately', () => {
        plugin.toggle();
        plugin.dispose();
        expect(player.el().querySelector('.vjs-ab-loop-button')).toBeNull();
        expect(player.el().querySelector('.vjs-video-loop-button')).toBeNull();
        const event = new MouseEvent('mousedown', { bubbles: true, cancelable: true });
        plugin.seekBar.el().dispatchEvent(event);
        expect(player.el().querySelector('.vjs-ab-loop-overlay')).toBeNull();
    });
    it('toggles native full-video repeat from its control bar button', () => {
        plugin.loopButton.el().click();
        expect(player.loop()).toBe(true);
        expect(player.el().querySelector('video').loop).toBe(true);
        expect(plugin.loopButton.el().getAttribute('aria-pressed')).toBe('true');
        expect(plugin.loopButton.hasClass('vjs-ab-loop-enabled')).toBe(true);
        plugin.loopButton.el().click();
        expect(player.loop()).toBe(false);
        expect(plugin.loopButton.el().getAttribute('aria-pressed')).toBe('false');
    });
    it('keeps full-video repeat and A–B looping mutually exclusive', () => {
        plugin.toggle(); plugin.select(20); plugin.select(60);
        plugin.loopButton.el().click();
        expect(plugin.phase).toBe('off');
        expect(plugin.overlay.hidden).toBe(true);
        expect(player.loop()).toBe(true);
        plugin.toggle();
        expect(player.loop()).toBe(false);
        expect(plugin.phase).toBe('start');
        plugin.loopButton.el().click();
        expect(plugin.phase).toBe('off');
        player.trigger('loadstart');
        expect(player.loop()).toBe(true);
    });
});
