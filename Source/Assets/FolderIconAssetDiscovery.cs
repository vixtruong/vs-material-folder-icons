using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MaterialFolderIcons.VisualStudio.Assets
{
    internal static class FolderIconAssetDiscovery
    {
        public static FolderIconAssetCatalog Discover(string packageRoot)
        {
            if (string.IsNullOrWhiteSpace(packageRoot))
            {
                throw new ArgumentException("Package root must be provided.", nameof(packageRoot));
            }

            var closedDirectory = Path.Combine(packageRoot, "assets", "icons", "folders");
            var openDirectory = Path.Combine(packageRoot, "assets", "icons", "foldersOpen");

            return new FolderIconAssetCatalog(
                packageRoot,
                closedDirectory,
                openDirectory,
                EnumerateSvgKeys(closedDirectory),
                EnumerateSvgKeys(openDirectory));
        }

        private static IReadOnlyDictionary<string, string> EnumerateSvgKeys(string directory)
        {
            var icons = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (!Directory.Exists(directory))
            {
                return icons;
            }

            foreach (var path in Directory.EnumerateFiles(directory, "*.svg", SearchOption.TopDirectoryOnly).OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
            {
                var key = Path.GetFileNameWithoutExtension(path);
                if (!string.IsNullOrWhiteSpace(key) && !icons.ContainsKey(key))
                {
                    icons.Add(key, path);
                }
            }

            return icons;
        }
    }
}
