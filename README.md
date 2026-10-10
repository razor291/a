# CS.RIN.RU Enhanced for Playnite

A C# Playnite extension inspired by the supplied **CS.RIN.RU Enhanced (External)** userscript. It adds a custom game-details control with a **View on CS.RIN.RU** button, searches the forum for a likely topic, and opens the result in the system's default browser. It does not add forum tags to Playnite game names.

## Important UI note

Playnite's custom game-view controls are exposed for themes to embed. A theme must include the control named `CSRinEnhanced_CSRinButton` for the button to appear in the game details view. This is a Playnite theme integration requirement, not something an extension can force into every theme's layout.

Add this inside the desired panel/grid in your theme's game details XAML:

```xml
<ContentControl x:Name="CSRinEnhanced_CSRinButton" />
```

Use the exact name above. Place it where you want the control to appear.

## Requirements

- Windows
- Playnite with SDK-compatible plugin support
- Playnite on Windows
- For the GitHub Actions build route: a GitHub account (no local compiler or Visual Studio needed)

## Build without installing Visual Studio (GitHub Actions)

A Windows GitHub Actions workflow is included at `.github/workflows/build.yml`. It builds the project on GitHub-hosted Windows and uploads an install ZIP as a workflow artifact. No Visual Studio installation is needed on your PC, but you will need a GitHub account and must upload this source folder to a repository.

1. Create a new GitHub repository and upload the contents of this folder (including `.github/workflows/build.yml`).
2. Open the repository's **Actions** tab and select **Build Playnite extension**.
3. Click **Run workflow** (or wait for the first push build to finish).
4. Open the completed workflow run and download the `CSRinEnhanced-Playnite-install` artifact.
5. Extract that ZIP; it should contain `CSRinEnhanced.dll` and `extension.yaml`.

If the build fails, open the workflow run and inspect the build log; this project has not yet been verified by a successful remote build.

## Build with Visual Studio (alternative)

1. Open `CSRinEnhanced.csproj` in Visual Studio.
2. Restore NuGet packages.
3. Build the project in Release configuration.
4. Use the output folder containing `CSRinEnhanced.dll` and `extension.yaml`.

The project targets .NET Framework 4.6.2, as required by the Playnite plugin SDK.

## Install

1. Build/download the install ZIP and extract it.
2. In Playnite, open **Settings → For developers**.
3. Add the extracted folder (the directory containing `CSRinEnhanced.dll` and `extension.yaml`) to **External extensions**.
4. Restart Playnite.
5. Update your theme with the custom-control snippet above, then reload/restart Playnite.

For a normal installation, copy the output files into a new folder under:
- Installed Playnite: `%AppData%\Playnite\Extensions\CSRinEnhanced`
- Portable Playnite: `<Playnite folder>\Extensions\CSRinEnhanced`

Restart Playnite after installing.

## Matching behavior

- If a game has a Steam store URL in its Playnite links, the extension extracts the numeric Steam App ID and searches CS.RIN.RU's forum section 10.
- If that search has no usable topic links, it retries with the App ID plus developer information.
- If no Steam App ID is available, it searches using the game's name instead. It deliberately does **not** treat Playnite's internal game GUID as a Steam App ID.
- It prefers a topic title containing the game's name; otherwise it opens the first topic returned by the forum search.
- If no topic can be parsed, it opens a forum search URL.
- If the forum responds with a security check/CAPTCHA, the extension stops automatic matching and opens the fallback search page. It does not try to bypass the check.
- Requests time out after 15 seconds. HTTP/network errors are logged and produce a fallback search.

## Limitations

- CS.RIN.RU may change its HTML, apply anti-bot checks, or block automated requests. When that happens, automatic matching may fall back to a search page.
- The forum search is heuristic; the first result is not guaranteed to be the correct game topic.
- Steam App ID detection depends on a Steam store URL being present in the game's links. If your Playnite Steam library metadata does not expose that URL, name-based search is used.
- This is a source project, not a precompiled DLL. Build it on your PC before installing.
