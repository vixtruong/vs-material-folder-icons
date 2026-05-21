using System;
using System.Collections.Generic;
using MaterialFolderIcons.VisualStudio.Assets;

namespace MaterialFolderIcons.VisualStudio.Resolution
{
    internal sealed class FolderIconResolver
    {
        private readonly FolderIconAssetCatalog assetCatalog;
        private readonly IReadOnlyDictionary<string, string> aliases;
        private readonly IReadOnlyDictionary<string, string> customMappings;

        public FolderIconResolver(
            FolderIconAssetCatalog assetCatalog,
            IReadOnlyDictionary<string, string> aliases,
            IReadOnlyDictionary<string, string> customMappings)
        {
            this.assetCatalog = assetCatalog ?? throw new ArgumentNullException(nameof(assetCatalog));
            this.aliases = aliases ?? throw new ArgumentNullException(nameof(aliases));
            this.customMappings = customMappings ?? throw new ArgumentNullException(nameof(customMappings));
        }

        public FolderIconResolution? Resolve(string? folderName)
        {
            var normalizedName = NormalizeFolderName(folderName);
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                return null;
            }

            if (TryResolveIconKey(normalizedName, out var iconKey) &&
                assetCatalog.TryGetClosedIconPath(iconKey, out var closedIconPath))
            {
                assetCatalog.TryGetOpenIconPath(iconKey, out var openIconPath);
                return new FolderIconResolution(normalizedName, iconKey, closedIconPath, openIconPath);
            }

            return null;
        }

        public static string NormalizeFolderName(string? folderName)
        {
            return (folderName ?? string.Empty).Trim();
        }

        private bool TryResolveIconKey(string normalizedFolderName, out string iconKey)
        {
            if (assetCatalog.ClosedIconKeys.Contains(normalizedFolderName))
            {
                iconKey = normalizedFolderName;
                return true;
            }

            if (customMappings.TryGetValue(normalizedFolderName, out iconKey) &&
                assetCatalog.ClosedIconKeys.Contains(iconKey))
            {
                return true;
            }

            if (aliases.TryGetValue(normalizedFolderName, out iconKey) &&
                assetCatalog.ClosedIconKeys.Contains(iconKey))
            {
                return true;
            }

            iconKey = string.Empty;
            return false;
        }
    }
}
