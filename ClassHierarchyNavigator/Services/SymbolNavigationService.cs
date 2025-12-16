using ClassHierarchyNavigator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using System.Threading;
using System.Threading.Tasks;

namespace ClassHierarchyNavigator.Services
{
    public static class SymbolNavigationService
    {
        public static async Task NavigateToTypeAsync(
            VisualStudioWorkspace workspace,
            LeveledSymbol leveledSymbol,
            Project project,
            CancellationToken cancellationToken)
        {
            if (leveledSymbol == null)
            {
                return;
            }

            if (project == null)
            {
                return;
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            await workspace.TryGoToDefinitionAsync(leveledSymbol.Symbol, project, cancellationToken);
        }
    }
}
