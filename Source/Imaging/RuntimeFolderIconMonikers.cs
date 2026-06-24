using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Resources;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MaterialFolderIcons.VisualStudio.Generated;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem;

namespace MaterialFolderIcons.VisualStudio.Imaging
{
    /// <summary>
    /// Registers packaged PNG resources in Visual Studio's in-memory image library.
    ///
    /// The VSIX image manifest remains the compatibility fallback, but it is not a
    /// reliable single source after Visual Studio Installer rebuilds its component
    /// cache. Runtime handles make the monikers valid again on every IDE process and
    /// must remain strongly referenced for as long as those monikers are in use.
    /// </summary>
    internal static class RuntimeFolderIconMonikers
    {
        private const string ResourceAssemblyName = "MaterialFolderIcons.VisualStudio";
        private static readonly ResourceManager EmbeddedResources =
            new ResourceManager(ResourceAssemblyName + ".g", typeof(RuntimeFolderIconMonikers).Assembly);
        private static readonly object Gate = new object();
        private static readonly Dictionary<string, IImageHandle> ClosedHandles =
            new Dictionary<string, IImageHandle>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, IImageHandle> OpenHandles =
            new Dictionary<string, IImageHandle>(StringComparer.OrdinalIgnoreCase);

        private static string? lastRegistrationError;

        public static bool TryGetClosed(string iconKey, out ProjectImageMoniker moniker)
        {
            return TryGetRuntimeMoniker(iconKey, "folders", ClosedHandles, out moniker) ||
                MaterialFolderIconMonikers.Closed.TryGetValue(iconKey, out moniker);
        }

        public static bool TryGetOpen(string iconKey, out ProjectImageMoniker moniker)
        {
            return TryGetRuntimeMoniker(iconKey, "foldersopen", OpenHandles, out moniker) ||
                MaterialFolderIconMonikers.Open.TryGetValue(iconKey, out moniker);
        }

        public static RuntimeImageRegistrationSnapshot GetSnapshot()
        {
            lock (Gate)
            {
                return new RuntimeImageRegistrationSnapshot(
                    ClosedHandles.Count,
                    OpenHandles.Count,
                    lastRegistrationError);
            }
        }

        private static bool TryGetRuntimeMoniker(
            string iconKey,
            string resourceFolder,
            IDictionary<string, IImageHandle> handles,
            out ProjectImageMoniker moniker)
        {
            moniker = new ProjectImageMoniker(Guid.Empty, 0);
            if (string.IsNullOrWhiteSpace(iconKey))
            {
                return false;
            }

            lock (Gate)
            {
                if (!handles.TryGetValue(iconKey, out var handle))
                {
                    try
                    {
                        var resourceKey = BuildResourceKey(resourceFolder, iconKey);
                        var image = LoadEmbeddedImage(resourceKey);
                        handle = ImageLibrary.Default.AddCustomImage(image, false);
                        if (handle == null)
                        {
                            lastRegistrationError = $"Visual Studio returned no image handle for '{resourceKey}'.";
                            return false;
                        }

                        handles.Add(iconKey, handle);
                        lastRegistrationError = null;
                    }
                    catch (Exception ex)
                    {
                        // The install-time image manifest is retained as a fallback. Do
                        // not cache the failure: ImageLibrary can be unavailable briefly
                        // during startup and the next tree calculation should retry.
                        lastRegistrationError = ex.Message;
                        return false;
                    }
                }

                var imageMoniker = handle.Moniker;
                moniker = new ProjectImageMoniker(imageMoniker.Guid, imageMoniker.Id);
                return imageMoniker.Guid != Guid.Empty && imageMoniker.Id != 0;
            }
        }

        private static string BuildResourceKey(string resourceFolder, string iconKey)
        {
            return $"generatedimagespng/{resourceFolder}/{iconKey}.png";
        }

        private static ImageSource LoadEmbeddedImage(string resourceKey)
        {
            var stream = EmbeddedResources.GetStream(resourceKey, CultureInfo.InvariantCulture);
            if (stream == null)
            {
                throw new FileNotFoundException("Embedded folder icon resource was not found.", resourceKey);
            }

            using (stream)
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
        }
    }

    internal sealed class RuntimeImageRegistrationSnapshot
    {
        public RuntimeImageRegistrationSnapshot(int closedIconCount, int openIconCount, string? lastError)
        {
            ClosedIconCount = closedIconCount;
            OpenIconCount = openIconCount;
            LastError = lastError;
        }

        public int ClosedIconCount { get; }

        public int OpenIconCount { get; }

        public string? LastError { get; }
    }
}
