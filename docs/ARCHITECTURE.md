# Architecture

## Goal

The extension is structured around a small, safe folder icon pipeline:

1. Discover bundled SVG assets.
2. Build validated icon key sets.
3. Resolve folder names to existing icon keys.
4. Keep unsupported or unknown folders on Visual Studio's default icons.
5. Isolate Visual Studio Solution Explorer integration so native replacement can be added without rewriting asset logic.

## Asset Discovery

`FolderIconAssetDiscovery` locates assets relative to the installed package assembly:

- `assets/icons/folders`
- `assets/icons/foldersOpen`

Only top-level `.svg` files are scanned. Keys are filenames without `.svg`, stored in case-insensitive dictionaries using `StringComparer.OrdinalIgnoreCase`.

The VSIX project includes only these two folders as content. It intentionally excludes:

- `assets/icons/files`
- `assets/icons/ui`

## Folder Resolution

`FolderIconResolver` receives an actual folder name from the future Solution Explorer integration layer. It trims the name and preserves dots, underscores, hyphens, and spaces.

Resolution order:

1. Exact match against `assets/icons/folders` keys.
2. Custom mapping JSON match, validated against `assets/icons/folders`.
3. Built-in alias match, validated against `assets/icons/folders`.
4. `null` when no valid match exists.

Unknown folders are never forced to a fallback custom icon. They are left to Visual Studio.

## Alias Strategy

Aliases are declared in `DefaultFolderIconAliases`. Each alias group lists one or more folder-name spellings and one or more candidate icon keys. The first existing candidate key wins. If none of the candidates exists in `assets/icons/folders`, the alias group is skipped.

This keeps aliases useful without allowing accidental mappings to missing SVG files.

## Custom Mapping JSON

`CustomFolderIconMappings` loads a top-level `folderIcons` object. Invalid JSON, missing files, blank entries, and missing target icon keys are logged as warnings and ignored. Visual Studio should not crash because of a malformed mapping file.

## Visual Studio Integration

`SolutionExplorerFolderIconService` enumerates loaded project hierarchies, identifies physical and virtual folder nodes, resolves their names, renders SVG assets to cached `HICON` handles, and attempts to apply them with public hierarchy properties. It also subscribes to solution and hierarchy events so project load, folder creation, rename, and invalidation can re-run icon application.

Microsoft documentation exposes:

- `VSHPROPID_IconHandle` and `VSHPROPID_OpenFolderIconHandle`
- `VSHPROPID_IconMonikerGuid` / `VSHPROPID_IconMonikerId`
- `VSHPROPID_OpenFolderIconMonikerGuid` / `VSHPROPID_OpenFolderIconMonikerId`
- CPS `IProjectTreeModifier` and project tree image moniker patterns for project systems

Those APIs are stable public hierarchy APIs, but a project hierarchy is still allowed to reject `SetProperty` calls from an external package. If that happens, the extension logs a warning and leaves Visual Studio's default folder icon in place. The extension does not use unsupported HWND tree-view painting, private CPS internals, project file edits, or fake placeholder icons.

## Open Folder Icons

Open folder SVGs are discovered and made available through `FolderIconResolution.OpenIconPath`. They are rendered and applied through `VSHPROPID_OpenFolderIconHandle` when the hierarchy accepts external icon handle properties.

## Supported Project Types

The intended first target is physical folders in C# SDK-style projects. Support depends on whether the active project hierarchy accepts external `VSHPROPID_IconHandle` and `VSHPROPID_OpenFolderIconHandle` updates.

## Fallback Behavior

Fallbacks are conservative:

- Missing asset directories log warnings.
- Missing custom mapping targets log warnings.
- Unknown folders return no custom icon.
- Visual Studio default folder behavior remains unchanged.
