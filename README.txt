NOVASTRAP — Windows Roblox launcher companion

FEATURES IN THIS SOURCE PACKAGE
- Dark WPF interface with navigation, theme presets and accent colours
- Roblox installation detection and normal client launch
- Game shortcut library with URL/place-ID validation
- Launch confirmation preference and optional close-after-launch behavior
- Auto-refresh of Roblox detection, remember-last-page, compact-window and always-on-top options
- Optional current-user Windows startup registration
- Optional local diagnostic logging and user-triggered diagnostic export
- Performance guidance presets (informational; they do not alter Roblox settings)
- Diagnostics, app settings JSON and data-folder actions

BUILD AN EXE WITHOUT RUNNING BUILD_AND_RUN.BAT
The .github/workflows/build-windows.yml workflow builds a self-contained Windows x64 NovaStrap.exe on GitHub Actions. Follow BUILD_WITH_GITHUB_ACTIONS.md. This does require uploading the project to a GitHub repository and running the workflow; no local .bat is required for that route.

SOURCE AND LIMITATIONS
- Main source code is in src\.
- Official Bloxstrap upstream reference and its MIT license notice are in UPSTREAM_AND_LICENSE.txt. This package does NOT contain a code-level merge of Bloxstrap source; do not claim that it does.
- FastFlags are informational and not applied. Mods are not patched into Roblox. Cursor Studio opens Windows cursor settings but does not replace the in-game cursor. Discord Rich Presence is not active.
- This environment did not have a .NET SDK or network access, so NovaStrap.exe could not be compiled or tested here. The first Windows GitHub Actions build is the first actual compiler validation.
- Independent project, not affiliated with Roblox Corporation or Bloxstrap maintainers. Unsigned binary builds may trigger Windows SmartScreen.
