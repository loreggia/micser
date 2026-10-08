# Release notes

<!--
Users see these notes: on the GitHub release, and in Micser under "What's new" (a downloaded update) and in the settings (the installed
version). Write them for users; changes that only affect development don't belong here.

Collect changes under "Unreleased". Before tagging vX.Y.Z, rename that heading to "## X.Y.Z" and commit; scripts/pack.ps1 takes the notes
from that section, and the release workflow fails without it.
-->

## Unreleased

## 0.10.0 - 2026-10-08

- Micser uses its own green accent colour instead of the default blue.
- A newly added module is selected right away. Before, it was sometimes selected only after the next change to the graph.
- Fixed the UI sometimes showing outdated data after connecting to the engine, and connections sometimes not being drawn between new
  modules.

## 0.9.0 - 2026-10-06

- "What's new" shows the release notes of a downloaded update, from the update button in the toolbar, and of the installed version, in the
  settings.
