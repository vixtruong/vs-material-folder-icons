# Release Notes

## 1.0.6 - 2026-05-27

### Fixed

- Fixed folder icons disappearing after installing or enabling some other Visual Studio extensions.
- Replaced the hierarchy fallback path that rendered SVG files into cached `HICON` handles with Visual Studio Image Service `ImageMoniker` updates.
- Stopped reapplying icons from `OnInvalidateIcon`, which could fight with Visual Studio and other extensions when the Solution Explorer icon cache was invalidated.
- Added rollback logic for hierarchy moniker updates so a folder node is restored if Visual Studio accepts only one side of the GUID/ID pair.
- Skipped hierarchy fallback updates unless the project hierarchy explicitly advertises `VSHPROPID_SupportsIconMonikers`.
- Hardened hierarchy traversal against invalid or unexpected item IDs from project systems or other extensions.

### Changed

- Bumped the extension and assembly version to `1.0.6`.
- Changed the Material folder image catalog GUID to `{E45580F0-0E9E-40CA-9F97-39517ECBC951}` to avoid stale Visual Studio image cache entries or possible catalog conflicts from older builds.
- Kept the CPS project tree provider as the primary icon path for C# SDK-style projects.
- Removed the unused `SvgIconHandleCache` renderer and the `System.Drawing` project reference.

### Packaging

- Release VSIX: `bin\Release\net472\MaterialFolderIcons.VisualStudio.vsix`
- Assembly version: `1.0.6.0`
- VSIX manifest version: `1.0.6`
- Packaged image manifest: `MaterialFolderIcons.imagemanifest`
- Packaged closed folder SVG assets: `377`
- Packaged open folder SVG assets: `377`
- Release VSIX SHA256: `2F2271DAD7960951D2B24C37AA853C46CD3368445E3628685A9631AF6B4EE35F`

### Upgrade Notes

- Uninstall older builds before installing `1.0.6`.
- If Visual Studio still shows stale or blank icons after the upgrade, run `devenv /updateConfiguration` once and restart Visual Studio.
- Re-test with the extensions that previously caused blank folder icons.

