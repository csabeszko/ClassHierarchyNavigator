using ClassHierarchyNavigator.Navigation;
using ClassHierarchyNavigator.Services;
using EnvDTE;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ClassHierarchyNavigator.Commands
{
    public abstract class AbstractNavigateCommand
    {
        private static readonly Guid CommandSet = new Guid("e7a05e4b-4166-49b3-b0dc-b48bf11304b2");

        private readonly NavigationDirection _navigationDirection;
        private readonly AsyncPackage _package;

        protected AbstractNavigateCommand(
            int commandId,
            NavigationDirection navigationDirection,
            AsyncPackage package,
            OleMenuCommandService commandService)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            if (commandService == null)
            {
                throw new ArgumentNullException(nameof(commandService));
            }

            _package = package;
            _navigationDirection = navigationDirection;

            var commandIdentifier = new CommandID(CommandSet, commandId);
            var menuCommand = new MenuCommand(Execute, commandIdentifier);
            commandService.AddCommand(menuCommand);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    await ExecuteAsync();
                }
                catch (Exception exception)
                {
                    ShowError(exception);
                }
            });
        }

        protected async Task ExecuteAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(CancellationToken.None);

            var asyncServiceProvider = (IAsyncServiceProvider)_package;

            var developmentToolsEnvironmentObject = await asyncServiceProvider.GetServiceAsync(typeof(SDTE)) as DTE;
            if (developmentToolsEnvironmentObject == null)
            {
                return;
            }

            var activeDocument = developmentToolsEnvironmentObject.ActiveDocument;
            if (activeDocument == null)
            {
                return;
            }

            var filePath = activeDocument.FullName;
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            var textSelection = activeDocument.Selection as TextSelection;
            if (textSelection == null)
            {
                return;
            }

            var caretLineNumber = textSelection.ActivePoint.Line;
            var caretColumnIndex = textSelection.ActivePoint.LineCharOffset - 1;

            if (caretLineNumber < 1)
            {
                return;
            }

            if (caretColumnIndex < 0)
            {
                return;
            }

            var componentModel = await _package.GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
            if (componentModel == null)
            {
                return;
            }

            var workspace = componentModel.GetService<VisualStudioWorkspace>();
            if (workspace == null)
            {
                return;
            }

            var solution = workspace.CurrentSolution;

            var document = solution.Projects
                .SelectMany(project => project.Documents)
                .FirstOrDefault(currentDocument => string.Equals(currentDocument.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

            if (document == null)
            {
                return;
            }

            var sourceText = await document.GetTextAsync(CancellationToken.None);

            if (caretLineNumber > sourceText.Lines.Count)
            {
                return;
            }

            var line = sourceText.Lines[caretLineNumber - 1];

            var clampedColumnIndex = caretColumnIndex;
            if (clampedColumnIndex > line.Span.Length)
            {
                clampedColumnIndex = line.Span.Length;
            }

            var caretPosition = line.Start + clampedColumnIndex;

            var navigator = new HierarchyNavigator();

            await navigator.NavigateAsync(
                workspace,
                _navigationDirection,
                document,
                caretPosition,
                CancellationToken.None);
        }

        private void ShowError(Exception exception)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var message = exception.Message;
            var title = EnumExtensions.GetDescription(_navigationDirection);

            VsShellUtilities.ShowMessageBox(
                _package,
                message,
                title,
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
