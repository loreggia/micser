# Release notes

<!--
Users see these notes: on the GitHub release, and in Micser under "What's new" (a downloaded update) and in the settings (the installed
version). Write them for users; changes that only affect development don't belong here.

Collect changes under "Unreleased". Before tagging vX.Y.Z, rename that heading to "## X.Y.Z" and commit; scripts/pack.ps1 takes the notes
from that section, and the release workflow fails without it.
-->

## Unreleased

- New "Filter" module: a high-pass or low-pass filter with an adjustable cutoff frequency and a slope of 12 to 48 dB per octave, e.g. to
  remove rumble from a microphone.

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
