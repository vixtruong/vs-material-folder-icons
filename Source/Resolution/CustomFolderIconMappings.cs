using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using MaterialFolderIcons.VisualStudio.Logging;

namespace MaterialFolderIcons.VisualStudio.Resolution
{
    internal static class CustomFolderIconMappings
    {
        public static async Task<IReadOnlyDictionary<string, string>> LoadAsync(string? jsonPath, ISet<string> availableIconKeys, ActivityLogLogger logger)
        {
            var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(jsonPath))
            {
                return mappings;
            }

            if (!File.Exists(jsonPath))
            {
                await logger.WarningAsync($"Custom mapping JSON was configured but does not exist: {jsonPath}");
                return mappings;
            }

            try
            {
                using var stream = File.OpenRead(jsonPath);
                var serializer = new DataContractJsonSerializer(typeof(CustomMappingDocument));
                if (serializer.ReadObject(stream) is not CustomMappingDocument document || document.FolderIcons == null)
                {
                    await logger.WarningAsync($"Custom mapping JSON is invalid or missing the folderIcons object: {jsonPath}");
                    return mappings;
                }

                foreach (var entry in document.FolderIcons)
                {
                    var folderName = FolderIconResolver.NormalizeFolderName(entry.Key);
                    var iconKey = FolderIconResolver.NormalizeFolderName(entry.Value);

                    if (string.IsNullOrWhiteSpace(folderName) || string.IsNullOrWhiteSpace(iconKey))
                    {
                        await logger.WarningAsync($"Ignoring invalid custom mapping entry in {jsonPath}.");
                        continue;
                    }

                    if (!availableIconKeys.Contains(iconKey))
                    {
                        await logger.WarningAsync($"Ignoring custom mapping '{folderName}' -> '{iconKey}' because the target SVG does not exist in assets/icons/folders.");
                        continue;
                    }

                    mappings[folderName] = iconKey;
                }
            }
            catch (Exception ex)
            {
                await logger.WarningAsync($"Failed to load custom mapping JSON '{jsonPath}': {ex.Message}");
            }

            return mappings;
        }

        [DataContract]
        private sealed class CustomMappingDocument
        {
            [DataMember(Name = "folderIcons")]
            public Dictionary<string, string>? FolderIcons { get; set; }
        }
    }
}
