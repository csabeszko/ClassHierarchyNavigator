using ClassHierarchyNavigator.Navigation;
using EnvDTE;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace ClassHierarchyNavigator
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class NavigateToBaseCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("e7a05e4b-4166-49b3-b0dc-b48bf11304b2");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="NavigateToBaseCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private NavigateToBaseCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static NavigateToBaseCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in NavigateToBaseCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new NavigateToBaseCommand(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _ = ThreadHelper.JoinableTaskFactory.RunAsync(async delegate
            {
                try
                {
                    await ExecuteAsync();
                }
                catch (Exception ex)
                {
                    string message = ex.Message;
                    string title = "NavigateToBaseCommand";
                    // Show a message box to prove we were here
                    VsShellUtilities.ShowMessageBox(
                        this.package,
                        message,
                        title,
                        OLEMSGICON.OLEMSGICON_CRITICAL,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                }
            });
        }

        private async Task ExecuteAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(CancellationToken.None);

            var serviceProvider = (IAsyncServiceProvider)package;

            var developmentToolsEnvironmentObject = await serviceProvider.GetServiceAsync(typeof(SDTE)) as DTE;
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
            if (caretLineNumber < 1 || caretColumnIndex < 0)
            {
                return;
            }

            var componentModel = await package.GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
            var workspace = componentModel?.GetService<VisualStudioWorkspace>();
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
                NavigationDirection.Base,
                document,
                caretPosition,
                CancellationToken.None);
        }
    }
}
