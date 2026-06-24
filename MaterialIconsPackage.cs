using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using MaterialFolderIcons.VisualStudio.Assets;
using MaterialFolderIcons.VisualStudio.Integration;
using MaterialFolderIcons.VisualStudio.Imaging;
using MaterialFolderIcons.VisualStudio.Logging;
using MaterialFolderIcons.VisualStudio.Options;
using MaterialFolderIcons.VisualStudio.Resolution;
using Microsoft.VisualStudio.Shell;

namespace MaterialFolderIcons.VisualStudio
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideAutoLoad(Microsoft.VisualStudio.Shell.Interop.UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideOptionPage(typeof(MaterialFolderIconsOptions), Constants.OptionsCategory, Constants.OptionsPageName, 0, 0, true)]
    [Guid(Constants.PackageGuidString)]
    public sealed class MaterialIconsPackage : AsyncPackage
    {
        private SolutionExplorerFolderIconService? solutionExplorerFolderIconService;

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            var logger = new ActivityLogLogger(this);
            await logger.InformationAsync("Initializing extension.");

            var options = (MaterialFolderIconsOptions)GetDialogPage(typeof(MaterialFolderIconsOptions));
            if (!options.EnableExtension)
            {
                await logger.InformationAsync("Extension is disabled from options.");
                return;
            }

            var packageRoot = System.IO.Path.GetDirectoryName(typeof(MaterialIconsPackage).Assembly.Location)
                ?? AppDomain.CurrentDomain.BaseDirectory;
            var catalog = FolderIconAssetDiscovery.Discover(packageRoot);

            await LogDiscoveryAsync(logger, catalog);

            if (!options.EnableFolderIconMapping)
            {
                await logger.InformationAsync("Folder icon mapping is disabled from options.");
                return;
            }

            var aliases = DefaultFolderIconAliases.Build(catalog.ClosedIconKeys);
            var customMappings = await CustomFolderIconMappings.LoadAsync(options.CustomMappingJsonPath, catalog.ClosedIconKeys, logger);
            var resolver = new FolderIconResolver(catalog, aliases, customMappings);

            solutionExplorerFolderIconService = new SolutionExplorerFolderIconService(this, logger);
            await solutionExplorerFolderIconService.InitializeAsync(catalog, resolver, cancellationToken);

            var runtimeImages = RuntimeFolderIconMonikers.GetSnapshot();
            if (runtimeImages.ClosedIconCount > 0)
            {
                await logger.InformationAsync(
                    $"Runtime image catalog is active with {runtimeImages.ClosedIconCount} closed and {runtimeImages.OpenIconCount} open folder icon(s). " +
                    "The handles are retained for the IDE process so installer cache rebuilds cannot invalidate these monikers.");
            }
            else if (!string.IsNullOrWhiteSpace(runtimeImages.LastError))
            {
                await logger.WarningAsync(
                    $"Runtime image registration was unavailable; using the packaged image manifest fallback: {runtimeImages.LastError}");
            }

            await logger.InformationAsync("CPS project tree icon provider is registered through the VSIX MEF component; hierarchy moniker fallback service is active.");
        }

        protected override void Dispose(bool disposing)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (disposing)
            {
                solutionExplorerFolderIconService?.Dispose();
                solutionExplorerFolderIconService = null;
            }

            base.Dispose(disposing);
        }

        private static async Task LogDiscoveryAsync(ActivityLogLogger logger, FolderIconAssetCatalog catalog)
        {
            if (catalog.ClosedIconKeys.Count == 0)
            {
                await logger.WarningAsync($"Missing or empty closed folder icon directory: {catalog.ClosedIconsDirectory}");
            }
            else
            {
                await logger.InformationAsync($"Discovered {catalog.ClosedIconKeys.Count} closed folder SVG icons from {catalog.ClosedIconsDirectory}.");
            }

            if (catalog.OpenIconKeys.Count == 0)
            {
                await logger.WarningAsync($"Missing or empty open folder icon directory: {catalog.OpenIconsDirectory}");
            }
            else
            {
                await logger.InformationAsync($"Discovered {catalog.OpenIconKeys.Count} open folder SVG icons from {catalog.OpenIconsDirectory}.");
            }
        }
    }
}
