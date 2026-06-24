using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MaterialFolderIcons.VisualStudio.Assets;
using MaterialFolderIcons.VisualStudio.Imaging;
using MaterialFolderIcons.VisualStudio.Logging;
using MaterialFolderIcons.VisualStudio.Resolution;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

#pragma warning disable VSSDK007

namespace MaterialFolderIcons.VisualStudio.Integration
{
    internal sealed class SolutionExplorerFolderIconService : IVsSolutionEvents, IDisposable
    {
        private readonly AsyncPackage package;
        private readonly ActivityLogLogger logger;
        private readonly List<HierarchySubscription> hierarchySubscriptions = new List<HierarchySubscription>();
        private FolderIconResolver? resolver;
        private IVsSolution? solution;
        private uint solutionEventsCookie;

        public SolutionExplorerFolderIconService(AsyncPackage package, ActivityLogLogger logger)
        {
            this.package = package;
            this.logger = logger;
        }

        public async Task InitializeAsync(FolderIconAssetCatalog catalog, FolderIconResolver folderIconResolver, CancellationToken cancellationToken)
        {
            resolver = folderIconResolver;
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            if (catalog.ClosedIconKeys.Count == 0)
            {
                await logger.WarningAsync("No closed folder SVG icons were discovered; custom folder icon resolution is disabled.");
                return;
            }

            if (catalog.OpenIconKeys.Count == 0)
            {
                await logger.WarningAsync("No open folder SVG icons were discovered; expanded-folder icon resolution is unavailable.");
            }

            solution = await package.GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
            if (solution == null)
            {
                await logger.WarningAsync("SVsSolution service is unavailable; folder icons cannot be applied.");
                return;
            }

            ErrorHandler.ThrowOnFailure(solution.AdviseSolutionEvents(this, out solutionEventsCookie));
            await ApplyToLoadedProjectsAsync(cancellationToken);
        }

        public void Dispose()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (solution != null && solutionEventsCookie != 0)
            {
                solution.UnadviseSolutionEvents(solutionEventsCookie);
                solutionEventsCookie = 0;
            }

            foreach (var subscription in hierarchySubscriptions)
            {
                subscription.Dispose();
            }

            hierarchySubscriptions.Clear();
        }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            SubscribeHierarchy(pHierarchy);
            ThreadHelper.JoinableTaskFactory.RunAsync(async () => await ApplyToHierarchyAsync(pHierarchy, CancellationToken.None)).FileAndForget("MaterialFolderIcons/OnAfterOpenProject");
            return VSConstants.S_OK;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            SubscribeHierarchy(pRealHierarchy);
            ThreadHelper.JoinableTaskFactory.RunAsync(async () => await ApplyToHierarchyAsync(pRealHierarchy, CancellationToken.None)).FileAndForget("MaterialFolderIcons/OnAfterLoadProject");
            return VSConstants.S_OK;
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            ThreadHelper.JoinableTaskFactory.RunAsync(async () => await ApplyToLoadedProjectsAsync(CancellationToken.None)).FileAndForget("MaterialFolderIcons/OnAfterOpenSolution");
            return VSConstants.S_OK;
        }

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            UnsubscribeHierarchy(pHierarchy);
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            UnsubscribeHierarchy(pRealHierarchy);
            return VSConstants.S_OK;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        private async Task ApplyToLoadedProjectsAsync(CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            if (solution == null)
            {
                return;
            }

            var onlyThisType = Guid.Empty;
            ErrorHandler.ThrowOnFailure(solution.GetProjectEnum((uint)__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION, ref onlyThisType, out var enumHierarchies));
            if (enumHierarchies == null)
            {
                return;
            }

            uint fetched;
            var hierarchies = new IVsHierarchy[1];
            while (enumHierarchies.Next(1, hierarchies, out fetched) == VSConstants.S_OK && fetched == 1)
            {
                SubscribeHierarchy(hierarchies[0]);

                try
                {
                    await ApplyToHierarchyAsync(hierarchies[0], cancellationToken);
                }
                catch (Exception ex)
                {
                    await logger.WarningAsync($"Skipping a project hierarchy because folder icon application failed: {ex.Message}");
                }
            }
        }

        private async Task ApplyToHierarchyAsync(IVsHierarchy hierarchy, CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            if (resolver == null || hierarchy == null)
            {
                return;
            }

            if (!SupportsIconMonikers(hierarchy, VSConstants.VSITEMID_ROOT))
            {
                return;
            }

            var matched = 0;
            var changed = 0;
            foreach (var itemId in EnumerateHierarchyItems(hierarchy, VSConstants.VSITEMID_ROOT))
            {
                if (itemId == VSConstants.VSITEMID_ROOT || !IsPhysicalFolder(hierarchy, itemId))
                {
                    continue;
                }

                var name = GetItemName(hierarchy, itemId);
                var resolution = resolver.Resolve(name);
                if (resolution == null)
                {
                    continue;
                }

                matched++;
                if (TryApplyIcon(hierarchy, itemId, resolution))
                {
                    changed++;
                }
            }

            if (changed > 0)
            {
                await logger.InformationAsync($"Applied Material folder icons to {changed} Solution Explorer folder node(s).");
            }
            else if (matched > 0)
            {
                await logger.WarningAsync($"Resolved {matched} folder icon mapping(s), but the project hierarchy did not accept custom icon properties. This project system may not allow external VSIX packages to replace existing folder icons.");
            }
        }

        private bool TryApplyIcon(IVsHierarchy hierarchy, uint itemId, FolderIconResolution resolution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                if (!RuntimeFolderIconMonikers.TryGetClosed(resolution.IconKey, out var closedMoniker))
                {
                    return false;
                }

                var closedApplied = TryApplyMoniker(
                    hierarchy,
                    itemId,
                    closedMoniker,
                    (int)__VSHPROPID8.VSHPROPID_IconMonikerGuid,
                    (int)__VSHPROPID8.VSHPROPID_IconMonikerId);

                var openApplied = false;
                if (closedApplied && RuntimeFolderIconMonikers.TryGetOpen(resolution.IconKey, out var openMoniker))
                {
                    openApplied = TryApplyMoniker(
                        hierarchy,
                        itemId,
                        openMoniker,
                        (int)__VSHPROPID8.VSHPROPID_OpenFolderIconMonikerGuid,
                        (int)__VSHPROPID8.VSHPROPID_OpenFolderIconMonikerId);
                }

                if (closedApplied || openApplied)
                {
                    RefreshHierarchyItem(hierarchy, itemId);
                }

                return closedApplied || openApplied;
            }
            catch (Exception ex)
            {
                ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                    await logger.WarningAsync($"Failed icon resolution/application for '{resolution.FolderName}' -> '{resolution.IconKey}': {ex.Message}")).FileAndForget("MaterialFolderIcons/TryApplyIcon");
                return false;
            }
        }

        private static bool SupportsIconMonikers(IVsHierarchy hierarchy, uint itemId)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return ErrorHandler.Succeeded(hierarchy.GetProperty(itemId, (int)__VSHPROPID8.VSHPROPID_SupportsIconMonikers, out var value)) &&
                IsTruthy(value);
        }

        private static bool IsTruthy(object? value)
        {
            if (value is bool boolValue)
            {
                return boolValue;
            }

            if (value is int intValue)
            {
                return intValue != 0;
            }

            if (value is short shortValue)
            {
                return shortValue != 0;
            }

            return false;
        }

        private static bool TryApplyMoniker(
            IVsHierarchy hierarchy,
            uint itemId,
            ProjectImageMoniker moniker,
            int monikerGuidPropertyId,
            int monikerIdPropertyId)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!TryReadMoniker(hierarchy, itemId, monikerGuidPropertyId, monikerIdPropertyId, out var originalGuid, out var originalId))
            {
                return false;
            }

            var idResult = hierarchy.SetProperty(itemId, monikerIdPropertyId, moniker.Id);
            var guid = moniker.Guid;
            var guidResult = ErrorHandler.Succeeded(idResult)
                ? hierarchy.SetGuidProperty(itemId, monikerGuidPropertyId, ref guid)
                : VSConstants.S_FALSE;

            if (ErrorHandler.Succeeded(idResult) && ErrorHandler.Succeeded(guidResult))
            {
                return true;
            }

            RestoreMoniker(hierarchy, itemId, monikerGuidPropertyId, monikerIdPropertyId, originalGuid, originalId);
            return false;
        }

        private static bool TryReadMoniker(
            IVsHierarchy hierarchy,
            uint itemId,
            int monikerGuidPropertyId,
            int monikerIdPropertyId,
            out Guid guid,
            out int id)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            guid = Guid.Empty;
            id = 0;

            if (ErrorHandler.Failed(hierarchy.GetGuidProperty(itemId, monikerGuidPropertyId, out guid)) ||
                ErrorHandler.Failed(hierarchy.GetProperty(itemId, monikerIdPropertyId, out var value)) ||
                !TryConvertToInt32(value, out id))
            {
                return false;
            }

            return true;
        }

        private static bool TryConvertToInt32(object? value, out int result)
        {
            result = 0;

            if (value is int intValue)
            {
                result = intValue;
                return true;
            }

            if (value is uint uintValue && uintValue <= int.MaxValue)
            {
                result = (int)uintValue;
                return true;
            }

            try
            {
                result = Convert.ToInt32(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void RestoreMoniker(
            IVsHierarchy hierarchy,
            uint itemId,
            int monikerGuidPropertyId,
            int monikerIdPropertyId,
            Guid originalGuid,
            int originalId)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            hierarchy.SetProperty(itemId, monikerIdPropertyId, originalId);
            hierarchy.SetGuidProperty(itemId, monikerGuidPropertyId, ref originalGuid);
        }

        private static IEnumerable<uint> EnumerateHierarchyItems(IVsHierarchy hierarchy, uint rootItemId)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var stack = new Stack<uint>();
            var visited = new HashSet<uint> { rootItemId };
            PushChildren(hierarchy, rootItemId, stack);

            while (stack.Count > 0)
            {
                var itemId = stack.Pop();
                if (!visited.Add(itemId))
                {
                    continue;
                }

                yield return itemId;
                PushChildren(hierarchy, itemId, stack);
            }
        }

        private static void PushChildren(IVsHierarchy hierarchy, uint itemId, Stack<uint> stack)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var child = GetItemIdProperty(hierarchy, itemId, (int)__VSHPROPID.VSHPROPID_FirstChild);
            while (child != VSConstants.VSITEMID_NIL)
            {
                stack.Push(child);
                child = GetItemIdProperty(hierarchy, child, (int)__VSHPROPID.VSHPROPID_NextSibling);
            }
        }

        private static uint GetItemIdProperty(IVsHierarchy hierarchy, uint itemId, int propertyId)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (ErrorHandler.Failed(hierarchy.GetProperty(itemId, propertyId, out var value)) || value == null)
            {
                return VSConstants.VSITEMID_NIL;
            }

            try
            {
                return value is int intValue ? unchecked((uint)intValue) : Convert.ToUInt32(value);
            }
            catch
            {
                return VSConstants.VSITEMID_NIL;
            }
        }

        private static string? GetItemName(IVsHierarchy hierarchy, uint itemId)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (ErrorHandler.Succeeded(hierarchy.GetProperty(itemId, (int)__VSHPROPID.VSHPROPID_Name, out var name)) &&
                name is string text)
            {
                return text;
            }

            return null;
        }

        private static bool IsPhysicalFolder(IVsHierarchy hierarchy, uint itemId)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (ErrorHandler.Failed(hierarchy.GetGuidProperty(itemId, (int)__VSHPROPID.VSHPROPID_TypeGuid, out var typeGuid)))
            {
                return false;
            }

            return typeGuid == VSConstants.GUID_ItemType_PhysicalFolder ||
                   typeGuid == VSConstants.GUID_ItemType_VirtualFolder;
        }

        private static void RefreshHierarchyItem(IVsHierarchy hierarchy, uint itemId)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (Package.GetGlobalService(typeof(SVsUIShell)) is IVsUIShell uiShell)
            {
                uiShell.UpdateCommandUI(0);
            }
        }

        private void SubscribeHierarchy(IVsHierarchy hierarchy)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (hierarchySubscriptions.Exists(subscription => ReferenceEquals(subscription.Hierarchy, hierarchy)))
            {
                return;
            }

            if (ErrorHandler.Succeeded(hierarchy.AdviseHierarchyEvents(new HierarchyEventSink(this, hierarchy), out var cookie)) && cookie != 0)
            {
                hierarchySubscriptions.Add(new HierarchySubscription(hierarchy, cookie));
            }
        }

        private void UnsubscribeHierarchy(IVsHierarchy hierarchy)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            for (var index = hierarchySubscriptions.Count - 1; index >= 0; index--)
            {
                if (ReferenceEquals(hierarchySubscriptions[index].Hierarchy, hierarchy))
                {
                    hierarchySubscriptions[index].Dispose();
                    hierarchySubscriptions.RemoveAt(index);
                }
            }
        }

        private sealed class HierarchySubscription : IDisposable
        {
            public HierarchySubscription(IVsHierarchy hierarchy, uint cookie)
            {
                Hierarchy = hierarchy;
                Cookie = cookie;
            }

            public IVsHierarchy Hierarchy { get; }

            private uint Cookie { get; set; }

            public void Dispose()
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (Cookie != 0)
                {
                    Hierarchy.UnadviseHierarchyEvents(Cookie);
                    Cookie = 0;
                }
            }
        }

        private sealed class HierarchyEventSink : IVsHierarchyEvents
        {
            private readonly SolutionExplorerFolderIconService owner;
            private readonly IVsHierarchy hierarchy;

            public HierarchyEventSink(SolutionExplorerFolderIconService owner, IVsHierarchy hierarchy)
            {
                this.owner = owner;
                this.hierarchy = hierarchy;
            }

            public int OnItemAdded(uint itemidParent, uint itemidSiblingPrev, uint itemidAdded)
            {
                ThreadHelper.JoinableTaskFactory.RunAsync(async () => await owner.ApplyToHierarchyAsync(hierarchy, CancellationToken.None)).FileAndForget("MaterialFolderIcons/OnItemAdded");
                return VSConstants.S_OK;
            }

            public int OnItemsAppended(uint itemidParent)
            {
                ThreadHelper.JoinableTaskFactory.RunAsync(async () => await owner.ApplyToHierarchyAsync(hierarchy, CancellationToken.None)).FileAndForget("MaterialFolderIcons/OnItemsAppended");
                return VSConstants.S_OK;
            }

            public int OnPropertyChanged(uint itemid, int propid, uint flags)
            {
                if (propid == (int)__VSHPROPID.VSHPROPID_Name || propid == (int)__VSHPROPID.VSHPROPID_Caption)
                {
                    ThreadHelper.JoinableTaskFactory.RunAsync(async () => await owner.ApplyToHierarchyAsync(hierarchy, CancellationToken.None)).FileAndForget("MaterialFolderIcons/OnPropertyChanged");
                }

                return VSConstants.S_OK;
            }

            public int OnInvalidateItems(uint itemidParent)
            {
                ThreadHelper.JoinableTaskFactory.RunAsync(async () => await owner.ApplyToHierarchyAsync(hierarchy, CancellationToken.None)).FileAndForget("MaterialFolderIcons/OnInvalidateItems");
                return VSConstants.S_OK;
            }

            public int OnInvalidateIcon(IntPtr hicon)
            {
                return VSConstants.S_OK;
            }

            public int OnItemDeleted(uint itemid)
            {
                return VSConstants.S_OK;
            }
        }
    }
}
