using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MaterialFolderIcons.VisualStudio.Assets
{
    internal sealed class FolderIconAssetCatalog
    {
        public FolderIconAssetCatalog(
            string packageRoot,
            string closedIconsDirectory,
            string openIconsDirectory,
            IReadOnlyDictionary<string, string> closedIconPaths,
            IReadOnlyDictionary<string, string> openIconPaths)
        {
            PackageRoot = packageRoot;
            ClosedIconsDirectory = closedIconsDirectory;
            OpenIconsDirectory = openIconsDirectory;
            ClosedIconPaths = new ReadOnlyDictionary<string, string>(CopyDictionary(closedIconPaths));
            OpenIconPaths = new ReadOnlyDictionary<string, string>(CopyDictionary(openIconPaths));
            ClosedIconKeys = new HashSet<string>(closedIconPaths.Keys, StringComparer.OrdinalIgnoreCase);
            OpenIconKeys = new HashSet<string>(openIconPaths.Keys, StringComparer.OrdinalIgnoreCase);
        }

        public string PackageRoot { get; }

        public string ClosedIconsDirectory { get; }

        public string OpenIconsDirectory { get; }

        public IReadOnlyDictionary<string, string> ClosedIconPaths { get; }

        public IReadOnlyDictionary<string, string> OpenIconPaths { get; }

        public ISet<string> ClosedIconKeys { get; }

        public ISet<string> OpenIconKeys { get; }

        public bool TryGetClosedIconPath(string iconKey, out string path)
        {
            return ClosedIconPaths.TryGetValue(iconKey, out path);
        }

        public bool TryGetOpenIconPath(string iconKey, out string path)
        {
            return OpenIconPaths.TryGetValue(iconKey, out path);
        }

        private static Dictionary<string, string> CopyDictionary(IReadOnlyDictionary<string, string> source)
        {
            var copy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in source)
            {
                copy[item.Key] = item.Value;
            }

            return copy;
        }
    }
}
