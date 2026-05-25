# Publishing to Visual Studio Marketplace

## Before Publishing

1. Update `source.extension.vsixmanifest`:
   - `Identity Version`
   - `Identity Publisher`
   - `DisplayName`
   - `Description`
   - `Tags`
   - `Icon`
2. Keep attribution files in the package:
   - `THIRD_PARTY_NOTICES.md` in the repository
   - `Resources\License.txt` in the VSIX
3. In the Marketplace overview, mention that bundled folder icons are derived from Atom Material Icons for JetBrains and link to `https://github.com/AtomMaterialUI/a-file-icon-idea`.
4. Build and test the VSIX locally:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\Generate-FolderIconAssets.ps1
dotnet build -c Release
```

The release VSIX is created at:

```text
bin\Release\net472\MaterialFolderIcons.VisualStudio.vsix
```

Double-click the VSIX or install it into a Visual Studio Experimental Instance before uploading.

## Publish With Marketplace UI

1. Go to https://marketplace.visualstudio.com.
2. Sign in with a Microsoft account.
3. Select **Publish extensions**.
4. Create or select a publisher.
5. Select **New extension** > **Visual Studio**.
6. Upload `bin\Release\net472\MaterialFolderIcons.VisualStudio.vsix`.
7. Confirm the details populated from `source.extension.vsixmanifest`.
8. Fill in:
   - Overview
   - Type
   - Categories, up to 3
   - Pricing category
   - Repository URL, if public
   - Q&A setting
9. Select **Save & Upload**.
10. Use **View Extension** to review the listing.
11. Select **Make Public** when ready.

## Publish With Command Line

Create a Marketplace publish manifest, for example `marketplace.publish.json`:

```json
{
  "$schema": "http://json.schemastore.org/vsix-publish",
  "categories": [ "coding" ],
  "identity": {
    "internalName": "MaterialFolderIcons.VisualStudio"
  },
  "overview": "README.md",
  "priceCategory": "free",
  "publisher": "YOUR_MARKETPLACE_PUBLISHER_ID",
  "private": true,
  "qna": true,
  "repo": "https://github.com/YOUR_ACCOUNT/YOUR_REPO"
}
```

Find `VsixPublisher.exe` under the Visual Studio SDK tools folder, typically:

```text
%VSINSTALLDIR%\VSSDK\VisualStudioIntegration\Tools\Bin\VsixPublisher.exe
```

Publish:

```powershell
& "$env:VSINSTALLDIR\VSSDK\VisualStudioIntegration\Tools\Bin\VsixPublisher.exe" publish `
  -payload "bin\Release\net472\MaterialFolderIcons.VisualStudio.vsix" `
  -publishManifest "marketplace.publish.json" `
  -personalAccessToken "YOUR_PAT"
```

Keep `"private": true` for the first upload if you want to inspect the listing before making it public.

## Update an Existing Listing

1. Increase `Identity Version` in `source.extension.vsixmanifest`.
2. Build Release again.
3. Upload the new VSIX from Marketplace UI, or rerun `VsixPublisher.exe publish`.
4. Review the listing.
5. Make it public if the upload was private.

## Notes

- The Marketplace logo is taken from `<Icon>` in `source.extension.vsixmanifest` when provided.
- This VSIX schema does not accept a `<License>` metadata element; keep license text packaged as `Resources\License.txt` and include attribution in the Marketplace overview.
- The folder icons are MIT-licensed third-party assets. Keep the copyright and permission notice in redistributed copies.
- For command-line publishing, the `publisher` in `marketplace.publish.json` must be the Marketplace publisher ID, not necessarily the display name.
- Do not commit a real personal access token.
