namespace MaterialFolderIcons.VisualStudio.Resolution
{
    internal sealed class FolderIconResolution
    {
        public FolderIconResolution(string folderName, string iconKey, string closedIconPath, string? openIconPath)
        {
            FolderName = folderName;
            IconKey = iconKey;
            ClosedIconPath = closedIconPath;
            OpenIconPath = openIconPath;
        }

        public string FolderName { get; }

        public string IconKey { get; }

        public string ClosedIconPath { get; }

        public string? OpenIconPath { get; }
    }
}
