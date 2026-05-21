using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace MaterialFolderIcons.VisualStudio.Options
{
    public sealed class MaterialFolderIconsOptions : DialogPage
    {
        [Category("General")]
        [DisplayName("Enable extension")]
        [Description("Enables initialization, asset discovery, and future Solution Explorer folder icon integration.")]
        public bool EnableExtension { get; set; } = true;

        [Category("General")]
        [DisplayName("Enable folder icon mapping")]
        [Description("Enables folder-name resolution against bundled Material folder SVG assets.")]
        public bool EnableFolderIconMapping { get; set; } = true;

        [Category("General")]
        [DisplayName("Enable open folder icons")]
        [Description("Prepared option for expanded-folder icons. Current Visual Studio integration is limited by public Solution Explorer APIs.")]
        public bool EnableOpenFolderIcons { get; set; } = true;

        [Category("Matching")]
        [DisplayName("Enable case-insensitive matching")]
        [Description("Folder names are matched with StringComparer.OrdinalIgnoreCase. This option is kept visible for clarity; disabling is not currently supported.")]
        public bool EnableCaseInsensitiveMatching { get; set; } = true;

        [Category("Matching")]
        [DisplayName("Use only existing bundled icons")]
        [Description("When enabled, mappings that target missing SVG keys are ignored.")]
        public bool UseOnlyIconsThatExistInAssets { get; set; } = true;

        [Category("Custom mappings")]
        [DisplayName("Custom mapping JSON path")]
        [Description("Optional path to a JSON file with a top-level folderIcons object. Missing icon keys are ignored and logged.")]
        public string CustomMappingJsonPath { get; set; } = string.Empty;

        [Category("Custom mappings")]
        [DisplayName("Custom icon folder path")]
        [Description("Reserved for a future version. The MVP uses only bundled assets/icons/folders and assets/icons/foldersOpen.")]
        public string CustomIconFolderPath { get; set; } = string.Empty;
    }
}
