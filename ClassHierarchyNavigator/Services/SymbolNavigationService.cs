using ClassHierarchyNavigator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClassHierarchyNavigator.Services
{
    public static class SymbolNavigationService
    {
        public static async Task NavigateToTypeAsync(
            VisualStudioWorkspace workspace,
            LeveledSymbol leveledSymbol,
            IEnumerable<Project> projects,
            CancellationToken cancellationToken)
        {
            if (leveledSymbol is null)
            {
                return;
            }

            if (projects is null)
            {
                return;
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            foreach (var project in projects)
            {
                if (project is not null)
                {
                    if(await workspace.TryGoToDefinitionAsync(leveledSymbol.Symbol, project, cancellationToken))
                    {
                        break;
                    }
                }
            }
        }
    }
}
