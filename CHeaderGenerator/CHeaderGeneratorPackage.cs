using CHeaderGenerator.CodeWriter;
using CHeaderGenerator.Data;
using CHeaderGenerator.Extensions;
using CHeaderGenerator.Parser;
using CHeaderGenerator.Parser.C;
using CHeaderGenerator.UI;
using CHeaderGenerator.UI.View;
using CHeaderGenerator.UI.ViewModel;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NLog;
using NLog.Config;
using NLog.Targets.Custom;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Design;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;

namespace CHeaderGenerator
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad("{f1536ef8-92ec-443c-9ed7-fdadf150da82}")]
    [Guid(GuidList.guidCHeaderGeneratorPkgString)]
    [ProvideOptionPage(typeof(CSourceFileOptions), "C Header Generator", "Options", 0, 0, true)]
    public sealed class CHeaderGeneratorPackage : Package
    {
        #region Private Data Members

        [Import]
        private IDialogService dlgService = null;

        [Import]
        private ICParserFactory parserFactory = null;

        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        private static DTE2 applicationObject = null;

        #endregion

        #region Constructor Definition

        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public CHeaderGeneratorPackage()
        {
            using (var mainCatalog = new AggregateCatalog())
            using (var container = new CompositionContainer(mainCatalog))
            {
                mainCatalog.Catalogs.Add(new AssemblyCatalog(this.GetType().Assembly));
                container.ComposeParts(this);
            }
        }

        #endregion

        #region Public Property Definitions

        private static DTE2 ApplicationObject
        {
            get
            {
                if (applicationObject == null)
                    applicationObject = (DTE2)Package.GetGlobalService(typeof(SDTE));
                return applicationObject;
            }
        }

        #endregion

        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            this.InitializeLogging();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                // Create the command for the menu item on the solution explorer.
                var menuCommandID = new CommandID(GuidList.SolutionExplorerCmdSet, (int)PkgCmdIDList.cmdidGenerateCHeader);
                var menuItem = new OleMenuCommand(this.GenerateCHeaderMenuItemCallback, menuCommandID);
                menuItem.Visible = true;
                mcs.AddCommand(menuItem);
                menuItem.BeforeQueryStatus += (sender, e) =>
                {
                    var item = sender as OleMenuCommand;
                    if (item != null)
                    {
                        item.Visible = true;

                        var solutionExplorer = ApplicationObject.ToolWindows.SolutionExplorer;
                        var selectedItems = solutionExplorer.SelectedItems as IEnumerable<UIHierarchyItem>;
                        foreach (var projectItem in selectedItems.GetProjectItems())
                        {
                            if (projectItem.Kind != EnvDTE.Constants.vsProjectItemKindPhysicalFile
                                || !Path.GetExtension(projectItem.FileNames[0]).Equals(".c", StringComparison.CurrentCultureIgnoreCase))
                            {
                                item.Visible = false;
                                break;
                            }
                        }
                    }
                };

                // Create the command for the menu item on the current document.
                menuCommandID = new CommandID(GuidList.CurrentDocumentCmdSet, (int)PkgCmdIDList.cmdidGenerateCHeaderCurrentDoc);
                menuItem = new OleMenuCommand(this.GenerateCHeaderCurrentDocMenuItemCallback, menuCommandID);
                menuItem.Visible = true;
                mcs.AddCommand(menuItem);
                menuItem.BeforeQueryStatus += (sender, e) =>
                {
                    var item = sender as OleMenuCommand;
                    if (item != null)
                    {
                        item.Visible = true;
                        var pItem = ApplicationObject.ActiveDocument.ProjectItem;
                        if (!Path.GetExtension(pItem.Properties.Item("FullPath").Value.ToString()).Equals(".c", StringComparison.CurrentCultureIgnoreCase))
                            item.Visible = false;
                    }
                };
            }
        }

        #endregion

        #region Callbacks

        /// <summary>
        /// This callback is used when a user selected one or more files from the solution explorer
        /// and clicks the Generate C Header menu item from the context menu.
        /// </summary>
        private void GenerateCHeaderMenuItemCallback(object sender, EventArgs e)
        {
            var selectedItems = ApplicationObject.ToolWindows.SolutionExplorer.SelectedItems
                as IEnumerable<UIHierarchyItem>;
            var projectItems = new List<ProjectItem>(selectedItems.GetProjectItems());

            this.ProcessFiles(projectItems);
        }

        /// <summary>
        /// This callback is used when a user right clicks an open document and clicks the
        /// Generate C Header menu item from the context menu.
        /// </summary>
        private void GenerateCHeaderCurrentDocMenuItemCallback(object sender, EventArgs e)
        {
            this.ProcessFiles(ApplicationObject.ActiveDocument.ProjectItem.ToList());
        }

        #endregion

        #region Private Method Definitions

        /// <summary>
        /// Initialize the NLog output window target.
        /// </summary>
        private void InitializeLogging()
        {
            using (var options = GetDialogPage(typeof(CSourceFileOptions)) as CSourceFileOptions)
            {
                var config = LogManager.Configuration ?? new LoggingConfiguration();

                var outputWindowTarget = new OutputWindowTarget()
                {
                    PaneName = "C Header Generator",
                    Layout = options.LogLayout
                };
                config.AddTarget("outputWindow", outputWindowTarget);

                var rule = new LoggingRule("*", NLog.LogLevel.FromString(options.LogLevel.ToString()), outputWindowTarget);
                config.LoggingRules.Add(rule);

                LogManager.Configuration = config;
            }
        }

        /// <summary>
        /// Generate C header files for a list of project items.
        /// </summary>
        /// <param name="projectItems">A list of project items.</param>
        private void ProcessFiles(IReadOnlyCollection<ProjectItem> projectItems)
        {
            bool showIncludeGuard;
            bool autoSaveFiles;
            string baseHeaderComment;
            var codeWriter = new CHeaderFileWriter();

            // Add Options
            using (var options = GetDialogPage(typeof(CSourceFileOptions)) as CSourceFileOptions)
            {
                codeWriter.IncludeStaticFunctions = options.IncludeStaticFunctions;
                codeWriter.IncludeExternFunctions = options.IncludeExternFunctions;
                codeWriter.HeaderCommentPlacement = options.HeaderCommentPlacement;
                showIncludeGuard = options.ShowIncludeGuard;
                autoSaveFiles = options.AutoSaveFiles;
                baseHeaderComment = ProcessHeaderCommentCommonTokens(options);
            }

            // Initialize viewmodel to keep track of progress
            using (var progressVM = new ProgressViewModel
                {
                    Minimum = 0.0,
                    Maximum = 1.0,
                    Message = "Starting...",
                    ProgressValue = 0.0
                })
            {
                this.ShowProgressDialog(progressVM);

                int i = 0;
                foreach (var projectItem in projectItems)
                {
                    if (projectItem.Document != null && !projectItem.Document.Saved && autoSaveFiles)
                        projectItem.Document.Save();

                    string file = projectItem.FileNames[0];
                    string itemToAdd = GetItemFileName(file);
                    bool error = false;

                    try
                    {
                        // Parse the file
                        log.Info("Processing {0}/{1}: {2}", ++i, projectItems.Count, file);
                        progressVM.Message = string.Format("{0}/{1}: Processing {2}", i, projectItems.Count, file);
                        if (!ParseItem(file, itemToAdd, codeWriter, showIncludeGuard, baseHeaderComment, projectItem))
                            break;
                        progressVM.ProgressValue = Convert.ToDouble(i);
                    }
                    catch (ParserException tex)
                    {
                        // Go to file/line where the error occurred and display a dialog with the error message.
                        var window = ApplicationObject.ItemOperations.OpenFile(file);
                        window.Activate();
                        if (tex.LineNumber > 0)
                        {
                            var textSelection = window.Selection as TextSelection;
                            if (textSelection != null)
                                textSelection.GotoLine(tex.LineNumber, true);
                        }
                        log.Error(string.Format("Failed to parse file: {0}", file), tex);
                        this.ShowExceptionDialog(tex, string.Format("Failed to parse file: {0}", file));
                        error = true;
                    }
                    catch (Exception ex)
                    {
                        // Show a dialog with a less-than-helpful exception message.
                        log.Error(string.Format("Unknown error while parsing file: {0}", file), ex);
                        this.ShowExceptionDialog(ex, string.Format("Unknown exception while parsing: {0}", file));
                        error = true;
                    }
                    finally
                    {
                        // Log the result of the parse operation.
                        var messageBuilder = new System.Text.StringBuilder();
                        messageBuilder.AppendFormat("Completed processing file {0}/{1}: {2}.", i, projectItems.Count, file);
                        if (error)
                            messageBuilder.Append(" There were one or more errors detected during processing.");
                        else
                            messageBuilder.Append(" The operation was completed successfully.");
                        log.Info(messageBuilder.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// Parse a C file and write a header files from the parsed contents.
        /// </summary>
        /// <param name="sourceFileName">The source file to parse</param>
        /// <param name="headerFileName">The header file to write</param>
        /// <param name="codeWriter">Code writer object</param>
        /// <param name="showIncludeGuard">Whether to surround the header file in an include guard</param>
        /// <param name="headerComment">The <see cref="CSourceFileOptions.HeaderComment"/> after having been processed with
        /// <see cref="ProcessHeaderCommentCommonTokens"/></param>
        /// <param name="projectItem">The project item corresponding to the source file</param>
        /// <returns></returns>
        private bool ParseItem(string sourceFileName, string headerFileName,
            CHeaderFileWriter codeWriter, bool showIncludeGuard, string headerComment, ProjectItem projectItem)
        {
            var containingProject = projectItem.ContainingProject;
            var existingItem = containingProject.FindExistingItem(headerFileName);
            if (existingItem != null)
            {
                var message = string.Format("File {0} already exists, would you like to re-generate the header file?", headerFileName);
                var result = this.dlgService.ShowYesNoCancelDialog(message,
                    "File Exists");
                if (result == System.Windows.MessageBoxResult.No)
                    return true;
                else if (result == System.Windows.MessageBoxResult.Cancel)
                    return false;
            }

            if (existingItem != null)
                CheckOutFile(ApplicationObject.SourceControl, headerFileName);

            var c = this.ParseSourceFile(sourceFileName);
            codeWriter.HeaderComment = ProcessHeaderCommentFileTokens(headerComment, containingProject, headerFileName);
            WriteToHeaderFile(showIncludeGuard, codeWriter, headerFileName, c);

            // Add File to Project
            if (existingItem == null)
            {
                containingProject.ProjectItems.AddFromFile(headerFileName);
                if (!containingProject.Saved)
                    containingProject.Save();
            }

            return true;
        }

        private void ShowProgressDialog(ProgressViewModel progressVM)
        {
            LaunchAction(() => this.dlgService.ShowDialog<ProgressDialog>(progressVM));
        }

        private void ShowExceptionDialog(Exception ex, string message)
        {
            LaunchAction(() => this.dlgService.ShowExceptionDialog(message, "Error", ex));
        }

        private static void LaunchAction(Action a)
        {
            // Launch dialog on new thread so it doesn't block the UI.
            // I would have preferred to use TPL, but it didn't seem to work,
            // may have had to do with setting thread as STA.
            var thread = new System.Threading.Thread(new ThreadStart(a));
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
        }

        /// <summary>
        /// Get header file name from source file name
        /// </summary>
        /// <param name="sourceFileName">The name of the source file</param>
        /// <returns>The corresponding header file name</returns>
        private static string GetItemFileName(string sourceFileName)
        {
            string extension = Path.GetFileName(sourceFileName).IsUpper() ? ".H" : ".h";

            return Path.Combine(Path.GetDirectoryName(sourceFileName),
                Path.GetFileNameWithoutExtension(sourceFileName) + extension);
        }

        /// <summary>
        /// Check out file from source control.
        /// </summary>
        /// <param name="sourceControl">Source control object</param>
        /// <param name="file">The name of the file to check out</param>
        private static void CheckOutFile(SourceControl sourceControl, string file)
        {
            if (sourceControl != null && sourceControl.IsItemUnderSCC(file)
                && !sourceControl.IsItemCheckedOut(file))
            {
                sourceControl.CheckOutItem(file);
            }
        }

        /// <summary>
        /// Writer the parsed source file to the output file
        /// </summary>
        /// <param name="showIncludeGuard">Whether to surround the header file in an include guard</param>
        /// <param name="codeWriter">The code writer object</param>
        /// <param name="headerFileName">The name of the header file to produce</param>
        /// <param name="c">The parse source file</param>
        private static void WriteToHeaderFile(bool showIncludeGuard, CHeaderFileWriter codeWriter, string headerFileName, CSourceFile c)
        {
            if (showIncludeGuard)
                codeWriter.IncludeGuard = new Regex(@"[^A-Z0-9_]").Replace(string.Format("__{0}__",
                    Path.GetFileName(headerFileName).ToUpperInvariant()), "_");
            else
                codeWriter.IncludeGuard = null;
            
            using (var stream = File.Open(headerFileName, FileMode.Create))
            {
                codeWriter.WriteHeaderFile(c, stream);
            }
        }

        /// <summary>
        /// Parse the given source file
        /// </summary>
        /// <param name="fileName">The name of the source file</param>
        /// <returns>A parsed C file</returns>
        private CSourceFile ParseSourceFile(string fileName)
        {
            using (var stream = File.OpenRead(fileName))
            {
                return this.parserFactory.CreateParser(stream).PerformParse();
            }
        }

        /// <summary>
        /// Generate a header comment using the value in the given options, replacing common tokens.
        /// </summary>
        /// <param name="options">The generator's options</param>
        /// <returns>The header comment with tokens replaced by the user's name, company, and date.</returns>
        private static string ProcessHeaderCommentCommonTokens(CSourceFileOptions options)
        {
            string headerComment = options.HeaderComment;
            
            if (!string.IsNullOrEmpty(headerComment))
            {
                string dateFormat = options.DateFormat;
                headerComment = headerComment
                    .CaseInsensitiveReplace("{Name}", GetUserName() ?? "{Name}")
                    .CaseInsensitiveReplace("{Date}", DateTime.Now.ToString(dateFormat))
                    .CaseInsensitiveReplace("{Company}", GetCompanyName() ?? "{Company}");
            } else {
                headerComment = null;
            }
            
            return headerComment;
        }

        /// <summary>
        /// Generate a header comment using the specified string, replacing file tokens.
        /// </summary>
        /// <param name="baseHeaderComment"></param>
        /// <param name="project"></param>
        /// <param name="headerFileName"></param>
        /// <returns>The header comment with tokens replaced by the user's name, company, and date.</returns>
        private static string ProcessHeaderCommentFileTokens(string baseHeaderComment, Project project, string headerFileName)
        {
            string headerComment = null;
            if (baseHeaderComment != null)
            {
                headerComment = baseHeaderComment.CaseInsensitiveReplace(
                    "{FileName}", Path.GetFileName(headerFileName)
                );
                if(baseHeaderComment.Contains("{RelativePath}", StringComparison.InvariantCultureIgnoreCase)) {
                    string relativePath = project.GetProjectRelativePath(headerFileName);
                    headerComment = baseHeaderComment.CaseInsensitiveReplace("{RelativePath}", relativePath);
                }
            }
            
            return headerComment;
        }

        /// <summary>
        /// Gets the company name of the executing assembly.
        /// </summary>
        /// <returns>The company name</returns>
        private static string GetCompanyName()
        {
            return ((AssemblyCompanyAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyCompanyAttribute), false))
                .Company;
        }

        /// <summary>
        /// Gets the current user's name
        /// </summary>
        /// <returns>The current user's name</returns>
        private static string GetUserName()
        {
            return UserPrincipal.Current.DisplayName ?? Environment.UserName;
        }

        #endregion
    }
}
