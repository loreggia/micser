# Release notes

<!--
Users see these notes: on the GitHub release, and in Micser under "What's new" (a downloaded update) and in the settings (the installed
version). Write them for users; changes that only affect development don't belong here.

Collect changes under "Unreleased". Before tagging vX.Y.Z, rename that heading to "## X.Y.Z" and commit; scripts/pack.ps1 takes the notes
from that section, and the release workflow fails without it.
-->

## Unreleased

- On Linux, the audio engine now runs with a realtime priority on standard systems too (through RTKit, like PipeWire), so audio is less
  likely to drop out while the computer is busy. Before, it only got one where the user was allowed realtime priorities.
- New "Noise gate" module: it lowers the audio by an adjustable range while it stays below a threshold, e.g. to remove keyboard and
  room noise between words from a microphone. Attack, hold and release set how fast it opens and closes, and the hysteresis keeps it
  from flickering at the threshold.
- The "Voice chat mic" template has a noise gate before its compressor. Subgraphs created from it can be updated to the new revision.
- New "Ducker" module: it lowers its input while the audio connected to its second input, "Sidechain", is above a threshold, e.g. music
  or game audio while you or a voice chat speaks. Depth sets how far it goes down; attack, hold and release how fast it goes down and
  comes back.
- New "Music ducking" template: a ducker whose sidechain goes through a high-pass filter, so that low rumble from a microphone doesn't
  lower the music. Connect the music to the ducker's input and the microphone or voice chat to the filter.
- New "Delay" module: it delays the audio by up to 2 seconds, e.g. to line up a Bluetooth and a wired output, or a microphone with
  a game capture for streaming. "Per channel" adds up to 100 ms more to single channels, e.g. to align speakers at different distances.
  Changing a delay fades over to the new one instead of clicking.
- New "Stereo tools" module for the left and right channel: swap them, put one of them or their average on both sides (e.g. for a
  microphone that only uses the left channel), make the stereo image narrower or wider, shift the balance, and invert the polarity of
  either side.

## 0.16.0 - 2026-10-13

- Micser runs on Linux: download `Micser.AppImage` from the release, make it executable and start it. It routes audio through PipeWire,
  creates the virtual audio cables itself (set their number and channel layouts in the settings), and updates itself as on Windows. In the
  settings' new "Desktop" section you can add Micser to the app menu and start it when you log in.
- Whether Micser starts when you log in ("Start with Windows" in the tray menu) can also be set in the settings, in the new "Desktop"
  section.
- The tray icon no longer shows notifications. A downloaded update is shown in the tray menu ("Restart to update") and in Micser, as
  before.
- The volume sliders of modules use the same scale as the system's volume control, which follows loudness: half way is now a quarter of
  the old level (-18 dB), and the low volumes that used to be squeezed into the first few percent can be set precisely. Saved volumes
  sound the same as before; only their percentages change.

## 0.15.0 - 2026-10-11

- The configuration can be exported to a file and imported again, e.g. as a backup or to move it to another computer: "Configuration" in
  the settings. The file has the modules, connections, subgraphs, your templates, the audio settings and the display preferences, and
  importing one replaces all of them. The configuration it replaces is backed up next to the configuration file.
- The theme can be chosen in the settings under "Display": the default theme, blue, purple or high contrast, each light or dark.
  "System" (the default) keeps the default theme light or dark like Windows, as before.

## 0.14.0 - 2026-10-11

- The Equalizer and Filter modules show a graph of their frequency response, which follows the settings as you change them.
- Modules can be resized with the corner at their bottom right. When a module is lower than its controls, they scroll; the Spectrum
  module's graph grows with the module. "Reset size" in the module's "More" menu returns it to its normal size.
- Module controls use the full width of the module.

## 0.13.0 - 2026-10-10

- New "Filter" module: a high-pass or low-pass filter with an adjustable cutoff frequency and a slope of 12 to 48 dB per octave, e.g. to
  remove rumble from a microphone. Its Q setting adds a resonant peak at the cutoff frequency.

## 0.12.0 - 2026-10-09

- Single channels can be routed on their own, e.g. the channels of an audio interface that has them all on one device. "Show channels" in a
  module's "More" menu, or the arrow between its inputs and outputs, adds a connector per channel, labelled with its speaker, below each
  port's connector; a connection can start or end at any of them. "Show the channels of new modules" in the settings turns it on for every
  module you add.
- Effects and the spectrum have a "Channels" setting in their "More" menu: Auto (as before), Mono, Stereo, Quad, 5.1, 7.1 or any number up to 64.
- A connection to a channel that a device doesn't have right now, e.g. while it's unplugged, is dashed and silent. It works again once the
  device is back.
- New subgraphs are green, matching the accent colour, instead of blue.

## 0.11.0 - 2026-10-08

- Internal maintenance; no user-visible changes.

## 0.10.0 - 2026-10-08

- Micser uses its own green accent colour instead of the default blue.
- A newly added module is selected right away. Before, it was sometimes selected only after the next change to the graph.
- Fixed the UI sometimes showing outdated data after connecting to the engine, and connections sometimes not being drawn between new
  modules.

## 0.9.0 - 2026-10-06

- "What's new" shows the release notes of a downloaded update, from the update button in the toolbar, and of the installed version, in the
  settings.
