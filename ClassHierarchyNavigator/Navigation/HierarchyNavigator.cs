using ClassHierarchyNavigator.Models;
using ClassHierarchyNavigator.Services;
using ClassHierarchyNavigator.UI;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ComponentModelHost;


namespace ClassHierarchyNavigator.Navigation
{
    public sealed class HierarchyNavigator
    {
        public async Task NavigateAsync(
            VisualStudioWorkspace workspace,
            NavigationDirection direction,
            Document document,
            int position,
            CancellationToken cancellationToken)
        {
            var typeSymbol = await TypeSymbolLocator.GetTypeSymbolAtPositionAsync(
                document,
                position,
                cancellationToken);

            if (typeSymbol == null)
            {
                return;
            }

            IReadOnlyList<LeveledSymbol> candidates;

            if (direction == NavigationDirection.Base)
            {
                candidates = BaseTypeProvider.GetBaseTypes(typeSymbol);
            }
            else
            {
                candidates = await DerivedTypeProvider.GetDerivedTypesAsync(
                    typeSymbol,
                    document.Project.Solution,
                    cancellationToken);
            }

            if (candidates.Count == 0)
            {
                return;
            }

            if (candidates.Count == 1)
            {
                await SymbolNavigationService.NavigateToTypeAsync(
                    workspace,
                    candidates[0],
                    document.Project,
                    cancellationToken);

                return;
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var textManager = (IVsTextManager)ServiceProvider.GlobalProvider.GetService(typeof(SVsTextManager));
            if (textManager == null)
            {
                return;
            }

            textManager.GetActiveView(1, null, out IVsTextView vsTextView);
            if (vsTextView == null)
            {
                return;
            }

            var componentModel = (IComponentModel)ServiceProvider.GlobalProvider.GetService(typeof(SComponentModel));
            if (componentModel == null)
            {
                return;
            }

            var editorAdaptersFactoryService = componentModel.GetService<IVsEditorAdaptersFactoryService>();
            if (editorAdaptersFactoryService == null)
            {
                return;
            }

            IWpfTextView wpfTextView = editorAdaptersFactoryService.GetWpfTextView(vsTextView);
            if (wpfTextView == null)
            {
                return;
            }

            if (position < 0 || position > wpfTextView.TextSnapshot.Length)
            {
                return;
            }

            var snapshotPoint = new SnapshotPoint(wpfTextView.TextSnapshot, position);
            var snapshotSpan = new SnapshotSpan(snapshotPoint, 0);

            var window = new TypeSelectionWindow(candidates, direction);
            bool preferAbove = direction == NavigationDirection.Base;
            WindowPositionHelper.PositionAroundSpan(window, wpfTextView, snapshotSpan, preferAbove);

            var result = window.ShowDialog();

            if (result == true && window.SelectedSymbol != null)
            {
                await SymbolNavigationService.NavigateToTypeAsync(
                    workspace,
                    window.SelectedSymbol,
                    document.Project,
                    cancellationToken);
            }
        }
    }
}
