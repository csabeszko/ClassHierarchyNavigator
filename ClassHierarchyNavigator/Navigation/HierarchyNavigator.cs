using ClassHierarchyNavigator.Models;
using ClassHierarchyNavigator.Services;
using ClassHierarchyNavigator.UI;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using System.Collections.Generic;
using System.Linq;
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

            if (candidates.Count == 1)
            {
                await SymbolNavigationService.NavigateToTypeAsync(
                    workspace,
                    candidates[0],
                    document.Project,
                    cancellationToken);

                return;
            }

            var warningText = CreateWarningText(workspace);
            var statusText = candidates.Count == 0 ? "No results." : null;

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var window = new TypeSelectionWindow(candidates, direction, typeSymbol, warningText, statusText);

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

        private static string? CreateWarningText(VisualStudioWorkspace workspace)
        {
            if (workspace == null)
            {
                return null;
            }

            var solution = workspace.CurrentSolution;
            if (solution == null)
            {
                return "Solution is not fully loaded. Results may be incomplete.";
            }

            var projectCount = solution.Projects.Count();
            if (projectCount == 0)
            {
                return "Solution is not fully loaded. Results may be incomplete.";
            }

            var projectsWithoutDocuments = solution.Projects.Where(x => x.Documents.Count() == 0).Take(1).ToList();
            if (projectsWithoutDocuments.Count > 0)
            {
                return "Some projects appear to be unloaded. Results may be incomplete.";
            }

            return null;
        }
    }
}
