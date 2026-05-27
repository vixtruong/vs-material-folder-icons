# Material Folder Icons for Visual Studio

Material Folder Icons for Visual Studio is a Visual Studio IDE VSIX extension project for Visual Studio 2022 17.14+ and Visual Studio 2026. It is not a VS Code extension and it does not create a VS Code Material Icon Theme package.

The extension focuses only on Solution Explorer folder icons. It ignores file icons, file extensions, `assets/icons/files`, and `assets/icons/ui`.

![Material Folder Icons overview](docs/images/folder-icons-overview.png)

## Status

This first pass implements the asset and resolution foundation:

- Scans `assets/icons/folders` for closed folder SVG icons.
- Scans `assets/icons/foldersOpen` for open folder SVG icons.
- Builds case-insensitive icon key sets using `StringComparer.OrdinalIgnoreCase`.
- Resolves folder names by exact key first, then validated aliases.
- Ignores aliases and custom mappings that target missing closed folder SVGs.
- Logs initialization, discovery counts, invalid custom mappings, and Visual Studio API limitations to the Visual Studio ActivityLog.
- Packages the folder SVG directories plus generated PNG image resources into the VSIX.

The current VSIX uses the Visual Studio Image Service for folder icons. It packages generated PNG resources through `MaterialFolderIcons.imagemanifest`, applies custom `ProjectImageMoniker` values through the CPS project tree provider, and uses the same moniker IDs in the hierarchy fallback path when a project hierarchy explicitly supports icon monikers:

- `VSHPROPID_IconMonikerGuid`
- `VSHPROPID_IconMonikerId`
- `VSHPROPID_OpenFolderIconMonikerGuid`
- `VSHPROPID_OpenFolderIconMonikerId`

Some Visual Studio project systems may reject those external `SetProperty` calls or not advertise `VSHPROPID_SupportsIconMonikers`. When that happens, the extension leaves Visual Studio's default folder icon in place.

## Visual Studio Support

- Target IDE: Visual Studio 2022 17.14+ and Visual Studio 2026 / `devenv.exe`
- VSIX installation target: stable Visual Studio API version 17.14+
- Language: C#
- Framework: .NET Framework 4.7.2
- Project type scope: C# SDK-style projects first, pending native Solution Explorer icon replacement support

## Folder Name Matching

Folder matching is intentionally conservative:

1. The actual folder name is trimmed.
2. Dots and underscores are preserved, so `.github` and `node_modules` remain meaningful names.
3. Exact lookup is attempted against actual SVG keys from `assets/icons/folders`.
4. Safe aliases are attempted next.
5. A result is returned only when the target closed folder SVG exists.
6. Unknown folders return `null` and keep Visual Studio's default behavior.

Example: if `assets/icons/folders/controllers.svg` exists, `Controllers` and `controllers` can resolve to `controllers`. If neither `controllers.svg` nor a validated alias target exists, the folder is ignored.

## Custom Mappings

The options page includes a future-ready custom mapping JSON path. The loader accepts this shape:

```json
{
  "folderIcons": {
    "Controllers": "controllers",
    "Helpers": "helper",
    "wwwroot": "wwwroot"
  }
}
```

If a target icon key does not exist in `assets/icons/folders`, that mapping is ignored and a warning is written to ActivityLog.

## Build

From a Developer PowerShell or terminal with the Visual Studio SDK workload available:

```powershell
dotnet build
```

The generated VSIX is emitted under `bin\Debug\net472`.

## Regenerate Folder Image Assets

When SVGs under `assets/icons/folders` or `assets/icons/foldersOpen` change, regenerate the packaged PNG image catalog assets:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\Generate-FolderIconAssets.ps1
```

The generator keeps the output canvas at 16x16, uses the script's `$Padding` value for visual sizing, and renders the SVG folder plus overlay glyph layers from the source files.

## Run In Experimental Instance

Open the solution in Visual Studio 2022 17.14+ or Visual Studio 2026 and use the standard VSIX debug profile, or run Visual Studio with the Experimental hive after building:

```powershell
devenv /rootsuffix Exp
```

Install the generated VSIX into the Experimental Instance when prompted by Visual Studio.

## Install VSIX

Build the project, then open the `.vsix` file from `bin\Debug\net472` with the Visual Studio VSIX Installer.

## Documentation

- [Architecture](docs/ARCHITECTURE.md)
- [Icon Asset Report](docs/ICON_ASSET_REPORT.md)
- [Manual Test Cases](docs/MANUAL_TESTS.md)
- [Publishing](docs/PUBLISHING.md)
- [Icon Preview](docs/icon-preview.html)

## License And Attribution

This extension code is licensed under the MIT License. See [LICENSE.txt](LICENSE.txt).

This extension includes folder icon assets derived from [Atom Material Icons for JetBrains](https://github.com/AtomMaterialUI/a-file-icon-idea), licensed under the MIT License.

The bundled SVG assets and generated PNG resources retain the upstream MIT copyright and permission notice. See [Third-Party Notices](THIRD_PARTY_NOTICES.md).

## Current Limitations

- Native replacement is attempted with public hierarchy icon properties, but C# SDK-style project systems can still reject external icon property changes.
- Open folder icons are scanned, included in the image manifest, and applied through `VSHPROPID_OpenFolderIconMonikerGuid` / `VSHPROPID_OpenFolderIconMonikerId` when the hierarchy accepts icon monikers.
- Visual Studio options are basic `DialogPage` options; a richer reset command and custom icon folder support are roadmap items.
- SVG files are packaged unchanged. No generated placeholder icons are created.

## Roadmap

- Confirm a first-class Visual Studio 2026 CPS integration point for existing C# SDK-style folder nodes if hierarchy `SetProperty` is rejected.
- Add an image-manifest or supported image-moniker pipeline if Visual Studio can consume the bundled folder art safely.
- Apply closed and open folder icons natively when the public API allows it.
- Add live refresh after folder rename, solution reload, and option changes.
- Add richer options UI with reset and custom icon folder validation.

## Screenshot

Screenshot placeholder: add a Solution Explorer screenshot here after native folder icon replacement is implemented and manually verified.
