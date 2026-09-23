# Video.js A–B loop

Import this directory and call `player.abLoop()` once. Arcadeia's `VideoPlayer`
initializes it automatically. Uses the Video.js 8 plugin and component APIs.

1. Press **A–B** in the control bar.
2. Click or tap the main progress bar to mark A, then B. Reverse selections are
   ordered automatically; points must be at least 0.1 seconds apart.
3. Playback repeats the highlighted range. If paused, press Play to begin.
4. Press **A–B** again to clear the range or cancel selection.

The adjacent **Loop entire video** button toggles Video.js's built-in full-video
repeat. Its arrow icon turns yellow while enabled. Enabling it clears any A–B
selection; starting an A–B selection turns full-video repeat off. Full-video
repeat remains enabled across source changes until toggled off.

Keyboard: activate A–B, use the progress bar's normal arrow keys to seek, and
press Enter or Space to mark each point. Escape on the progress bar clears the
loop. Seeking outside an active loop returns to A; clear it to seek freely.

The plugin resets when sources load and disables itself for unknown, infinite,
or zero durations. It requires the standard control bar and seek bar. Multiple
players have independent state; disposal removes listeners, UI, and timers.

The loop uses media seeks, timeupdate/seeked events, and a 30 ms playback check.
It is not sample-accurate or gapless; browser scheduling and media buffering can
affect the boundary. Native fullscreen controls do not display custom controls.

Run tests with `npm test -- src/plugins/video.js/videojs-ab-loop/index.test.js`
from `UI`. Tests use actual Video.js components with simulated media playback.
