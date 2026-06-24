using System;
using System.IO;
using System.Composition;
using MaterialFolderIcons.VisualStudio.Assets;
using MaterialFolderIcons.VisualStudio.Imaging;
using MaterialFolderIcons.VisualStudio.Resolution;
using Microsoft.VisualStudio.ProjectSystem;

namespace MaterialFolderIcons.VisualStudio.Cps
{
    [Export(typeof(IProjectTreePropertiesProvider))]
    [AppliesTo(ProjectCapabilities.CSharp)]
    [Order(1000)]
    internal sealed class MaterialFolderProjectTreePropertiesProvider : IProjectTreePropertiesProvider
    {
        private static readonly Lazy<FolderIconResolver> Resolver = new Lazy<FolderIconResolver>(CreateResolver);

        public void CalculatePropertyValues(
            IProjectTreeCustomizablePropertyContext propertyContext,
            IProjectTreeCustomizablePropertyValues propertyValues)
        {
            if (propertyContext == null ||
                propertyValues == null ||
                !propertyContext.IsFolder ||
                propertyValues.Flags.Contains(ProjectTreeFlags.Common.ProjectRoot))
            {
                return;
            }

            var folderName = GetFolderName(propertyContext.ItemName);
            var resolution = Resolver.Value.Resolve(folderName);
            if (resolution == null)
            {
                return;
            }

            if (RuntimeFolderIconMonikers.TryGetClosed(resolution.IconKey, out var closedMoniker))
            {
                propertyValues.Icon = closedMoniker;
            }

            if (RuntimeFolderIconMonikers.TryGetOpen(resolution.IconKey, out var openMoniker))
            {
                propertyValues.ExpandedIcon = openMoniker;
            }
        }

        private static FolderIconResolver CreateResolver()
        {
            var packageRoot = Path.GetDirectoryName(typeof(MaterialFolderProjectTreePropertiesProvider).Assembly.Location)
                ?? AppDomain.CurrentDomain.BaseDirectory;
            var catalog = FolderIconAssetDiscovery.Discover(packageRoot);
            var aliases = DefaultFolderIconAliases.Build(catalog.ClosedIconKeys);

            return new FolderIconResolver(
                catalog,
                aliases,
                new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        }

        private static string GetFolderName(string itemName)
        {
            if (string.IsNullOrWhiteSpace(itemName))
            {
                return string.Empty;
            }

            var trimmed = itemName.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var separatorIndex = trimmed.LastIndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
            return separatorIndex >= 0 ? trimmed.Substring(separatorIndex + 1) : trimmed;
        }
    }
}
