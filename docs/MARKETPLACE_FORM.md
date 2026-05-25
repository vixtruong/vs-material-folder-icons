# Visual Studio Marketplace Form Content

Use this file as copy-paste content when creating the publisher profile and the extension listing.

## Publisher Profile

### Basic Information

Name:

```text
Material Folder Icons
```

ID:

```text
material-folder-icons
```

Notes:

- The ID must be unique on Visual Studio Marketplace.
- Use only a stable ID you are willing to keep long term.
- If the ID is already taken, use one of:
  - `vix-material-folder-icons`
  - `material-folder-icons-vs`
  - `material-folder-icons-visualstudio`

### Verified Domain

Domain name:

```text
TODO: your verified website domain, for example https://your-domain.com
```

Leave this empty if you do not own a domain yet. Domain verification is optional, but useful for trust.

### About You

Description:

```text
Material Folder Icons publishes Visual Studio IDE extensions focused on improving Solution Explorer readability with clean, recognizable, Material-style folder icons.

The extensions are designed for developers who work with large solutions and want project folders such as Controllers, Configurations, Middleware, Helpers, logs, wwwroot, and other common directories to be easier to scan without changing file icons or project structure.
```

Logo:

```text
Use Resources\PublisherLogo.png.
```

Company website:

```text
TODO: https://your-domain.com
```

Support:

```text
TODO: support email or GitHub Issues URL
```

LinkedIn:

```text
TODO: optional LinkedIn profile/company URL
```

Source code repository:

```text
TODO: https://github.com/YOUR_ACCOUNT/MaterialIcons
```

Twitter/X:

```text
TODO: optional profile URL
```

## Extension Listing

Extension name:

```text
Material Folder Icons for Visual Studio
```

Internal name / ID:

```text
material-folder-icons
```

Alternative if already taken:

```text
material-folder-icons-vs
```

Version:

```text
0.7.0
```

Publisher:

```text
material-folder-icons
```

Short description:

```text
Material-style folder icons for Visual Studio Solution Explorer.
```

Full description:

```text
Material Folder Icons for Visual Studio improves Solution Explorer readability by applying Material-style icons to recognized project folders.

The extension focuses only on folder icons. It does not replace file icons, does not modify source files, and does not change the project structure. Folder names are matched conservatively against bundled icon assets and validated aliases, so unknown folders keep Visual Studio's default behavior.

This extension is built for Visual Studio 2022 17.14+ and Visual Studio 2026, and currently targets C# SDK-style project workflows first.
```

Categories:

```text
Coding
Themes
```

If `Themes` is not available in your Marketplace category dropdown, use only:

```text
Coding
```

Pricing category:

```text
Free
```

Q&A:

```text
Enabled
```

Repository:

```text
TODO: https://github.com/YOUR_ACCOUNT/MaterialIcons
```

Support URL:

```text
TODO: https://github.com/YOUR_ACCOUNT/MaterialIcons/issues
```

Tags:

```text
Visual Studio
Solution Explorer
Folder Icons
Material Icons
VSIX
C#
VS 2026
```

Enter these as separate tags. Do not paste the whole list as one tag, because Marketplace validates each tag length and can reject a combined string.

## Marketplace Overview

```markdown
# Material Folder Icons for Visual Studio

Material Folder Icons for Visual Studio improves Solution Explorer readability by applying Material-style icons to recognized project folders.

This extension is for the Visual Studio IDE. It is not a Visual Studio Code icon theme.

## Features

- Applies Material-style folder icons in Visual Studio Solution Explorer.
- Supports closed and open folder icon assets.
- Recognizes common folder names such as `Controllers`, `Configurations`, `Constants`, `Extensions`, `Files`, `Helper`, `Installers`, `logs`, `Middlewares`, and `wwwroot`.
- Uses case-insensitive folder name matching.
- Preserves meaningful folder names such as `.github` and `node_modules`.
- Keeps Visual Studio default behavior for unknown or unsupported folders.
- Focuses only on folder icons and does not replace file icons.
- Does not modify project files, source files, or folder structure.
- Packages bundled SVG folder assets and generated image resources inside the VSIX.

## Compatibility

- Visual Studio 2022 17.14+ and Visual Studio 2026
- .NET Framework 4.7.2 runtime requirement
- C# SDK-style projects are the primary tested project type

## Current Limitations

Visual Studio project systems can restrict external icon replacement APIs. When a project system rejects icon updates, the extension logs the limitation instead of changing unrelated behavior.

## License And Attribution

This extension includes folder icon assets derived from [Atom Material Icons for JetBrains](https://github.com/AtomMaterialUI/a-file-icon-idea), licensed under the MIT License.

The bundled SVG folder icons and generated PNG resources retain the upstream copyright and permission notice.

Upstream copyright:

Copyright (c) 2015-2024 Elior "Mallowigi" Boukhobza

See the packaged `Resources/License.txt` and repository `THIRD_PARTY_NOTICES.md` for full third-party license text and attribution.
```

## License Field / License Text

If the Marketplace asks for a license URL, use:

```text
TODO: https://github.com/YOUR_ACCOUNT/MaterialIcons/blob/main/LICENSE.txt
```

If it asks for license name:

```text
MIT License
```

Recommended repository license file:

```text
MIT License
```

Recommended note:

```text
This extension code is licensed under the MIT License. It also contains third-party icon assets derived from Atom Material Icons for JetBrains under the MIT License. The third-party copyright and permission notice are preserved in THIRD_PARTY_NOTICES.md and Resources/License.txt.
```

## Copyright / Legal Notice

```text
Material Folder Icons for Visual Studio includes folder icon assets derived from Atom Material Icons for JetBrains:
https://github.com/AtomMaterialUI/a-file-icon-idea

Atom Material Icons for JetBrains is licensed under the MIT License.
Copyright (c) 2015-2024 Elior "Mallowigi" Boukhobza.

The MIT copyright and permission notice is included with this extension in Resources/License.txt and in the repository file THIRD_PARTY_NOTICES.md.
```

## Publish Manifest Template

```json
{
  "$schema": "http://json.schemastore.org/vsix-publish",
  "categories": [
    "coding"
  ],
  "identity": {
    "internalName": "material-folder-icons"
  },
  "overview": "README.md",
  "priceCategory": "free",
  "publisher": "material-folder-icons",
  "private": true,
  "qna": true,
  "repo": "https://github.com/YOUR_ACCOUNT/MaterialIcons"
}
```

Keep `"private": true` for the first upload so you can review the Marketplace page before making it public.
