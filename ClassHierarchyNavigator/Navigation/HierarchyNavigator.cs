using ClassHierarchyNavigator.Models;
using ClassHierarchyNavigator.Services;
using ClassHierarchyNavigator.UI;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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

            var window = new TypeSelectionWindow(candidates, direction, typeSymbol);

            WindowPositionHelper.CenterOnCurrentMonitor(window);

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
