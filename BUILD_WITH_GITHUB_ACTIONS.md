NOVASTRAP — AUTOMATIC WINDOWS EXE BUILDS (NO LOCAL BAT)

This project has a GitHub Actions workflow at .github/workflows/build-windows.yml. Once the source is in a GitHub repository, each push to main/master (and a manually triggered workflow run) will:
- build a self-contained Windows x64 NovaStrap.exe on a Windows runner;
- attach the EXE to a GitHub Actions artifact; and
- automatically create a GitHub Release containing NovaStrap.exe.

ONE-TIME SETUP
1. Create a repository at https://github.com/new.
2. Extract this ZIP. Upload the CONTENTS of the NovaStrap folder to the repository root, including the hidden .github directory and its workflows/build-windows.yml file. A git client is the most reliable way to preserve the hidden workflow directory.
3. In the repository settings, open Actions > General and ensure workflows are allowed to run. For repository permissions, allow workflows to create releases (the workflow requests contents: write).
4. Push/commit to main or master. The build then runs automatically; you do not run BUILD_AND_RUN.bat on your PC.
5. When the run succeeds, open the repository's Releases page and download NovaStrap.exe. Each successful push publishes a new release with a unique build tag and updates the latest-release pointer.

NOTES
- This does not magically publish into a repository you have not created or authorized. The one-time repository upload/push is required to start the hosted build.
- The executable is self-contained for Windows x64, but is not code-signed. Windows SmartScreen may display a warning for unsigned software.
- This ChatGPT build environment had no .NET SDK or outbound network access, so the EXE has not been compiled or tested here. The first hosted Actions run provides the real Windows compile result; see its logs if it fails.
- The project is an independent NovaStrap implementation. The source code of Bloxstrap has not been merged into this build. The upstream/licence reference is documented separately.
