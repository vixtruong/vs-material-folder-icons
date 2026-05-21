using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace MaterialFolderIcons.VisualStudio.Logging
{
    internal sealed class ActivityLogLogger
    {
        private readonly AsyncPackage package;
        private IVsActivityLog? activityLog;

        public ActivityLogLogger(AsyncPackage package)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
        }

        public Task InformationAsync(string message)
        {
            return LogAsync(__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION, message);
        }

        public Task WarningAsync(string message)
        {
            return LogAsync(__ACTIVITYLOG_ENTRYTYPE.ALE_WARNING, message);
        }

        public Task ErrorAsync(string message)
        {
            return LogAsync(__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR, message);
        }

        private async Task LogAsync(__ACTIVITYLOG_ENTRYTYPE entryType, string message)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            activityLog ??= await package.GetServiceAsync(typeof(SVsActivityLog)) as IVsActivityLog;
            activityLog?.LogEntry((uint)entryType, Constants.ExtensionName, message);
        }
    }
}
