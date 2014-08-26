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
                // Create the command for the menu item.
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

                        var applicationObject = (DTE2)Package.GetGlobalService(typeof(SDTE));
                        var solutionExplorer = applicationObject.ToolWindows.SolutionExplorer;
                        var selectedItems = solutionExplorer.SelectedItems as IEnumerable<UIHierarchyItem>;
                        foreach (var projectItem in GetProjectItems(selectedItems))
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
                        var applicationObject = (DTE2)Package.GetGlobalService(typeof(SDTE));
                        var pItem = applicationObject.ActiveDocument.ProjectItem;
                        if (!Path.GetExtension(pItem.Properties.Item("FullPath").Value.ToString()).Equals(".c", StringComparison.CurrentCultureIgnoreCase))
                            item.Visible = false;
                    }
                };
            }
        }

        #endregion

        #region Callbacks

        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void GenerateCHeaderMenuItemCallback(object sender, EventArgs e)
        {
            var applicationObject = Package.GetGlobalService(typeof(SDTE)) as DTE2;
            var selectedItems = applicationObject.ToolWindows.SolutionExplorer.SelectedItems
                as IReadOnlyCollection<UIHierarchyItem>;
            var projectItems = new List<ProjectItem>(GetProjectItems(selectedItems));

            ProcessFiles(applicationObject, projectItems);
        }

        private void GenerateCHeaderCurrentDocMenuItemCallback(object sender, EventArgs e)
        {
            var applicationObject = Package.GetGlobalService(typeof(SDTE)) as DTE2;
            var projectItem = applicationObject.ActiveDocument.ProjectItem;

            var projectItemList = new List<ProjectItem>();
            projectItemList.Add(projectItem);

            ProcessFiles(applicationObject, projectItemList);
        }

        #endregion

        #region Private Method Definitions

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

        private void ProcessFiles(DTE2 applicationObject, IReadOnlyCollection<ProjectItem> projectItems)
        {
            bool showIncludeGuard;
            bool autoSaveFiles;
            var codeWriter = new CHeaderFileWriter();

            // Add Options
            using (var options = GetDialogPage(typeof(CSourceFileOptions)) as CSourceFileOptions)
            {
                codeWriter.HeaderComment = SetHeaderComment(options.HeaderComment);
                codeWriter.IncludeStaticFunctions = options.IncludeStaticFunctions;
                codeWriter.IncludeExternFunctions = options.IncludeExternFunctions;
                showIncludeGuard = options.ShowIncludeGuard;
                autoSaveFiles = options.AutoSaveFiles;
            }

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
                    if (projectItem.Document != null &&!projectItem.Document.Saved && autoSaveFiles)
                        projectItem.Document.Save();

                    string file = projectItem.FileNames[0];
                    string itemToAdd = GetItemFileName(file);
                    bool error = false;

                    try
                    {
                        i++;
                        log.Info("Processing {0}/{1}: {2}", i, projectItems.Count, file);
                        progressVM.Message = string.Format("{0}/{1}: Processing {2}", i, projectItems.Count, file);
                        if (!ParseItem(applicationObject, file, itemToAdd, codeWriter, showIncludeGuard, projectItem))
                            break;
                        progressVM.ProgressValue = Convert.ToDouble(i);
                    }
                    catch (ParserException tex)
                    {
                        var window = applicationObject.ItemOperations.OpenFile(file);
                        window.Activate();
                        if (tex.LineNumber > 0)
                        {
                            var textSelection = window.Selection as TextSelection;
                            if (textSelection != null)
                                textSelection.GotoLine(tex.LineNumber, true);
                        }
                        log.ErrorException(string.Format("Failed to parse file: {0}", file), tex);
                        this.ShowExceptionDialog(tex, string.Format("Failed to parse file: {0}", file));
                        error = true;
                    }
                    catch (Exception ex)
                    {
                        log.ErrorException(string.Format("Unknown error while parsing file: {0}", file), ex);
                        this.ShowExceptionDialog(ex, string.Format("Unknown exception while parsing: {0}", file));
                        error = true;
                    }
                    finally
                    {
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

        private static IEnumerable<ProjectItem> GetProjectItems(IEnumerable<UIHierarchyItem> hItems)
        {
            foreach (var hItem in hItems)
                yield return hItem.Object as ProjectItem;
        }

        private bool ParseItem(DTE2 applicationObject, string fileName, string itemToAdd,
            CHeaderFileWriter codeWriter, bool showIncludeGuard, ProjectItem projectItem)
        {
            var containingProject = projectItem.ContainingProject;
            var existingItem = FindExistingItem(itemToAdd, containingProject);
            if (existingItem != null)
            {
                var message = string.Format("File {0} already exists, would you like to re-generate the header file?", itemToAdd);
                var result = this.dlgService.ShowYesNoCancelDialog(message,
                    "File Exists");
                if (result == System.Windows.MessageBoxResult.No)
                    return true;
                else if (result == System.Windows.MessageBoxResult.Cancel)
                    return false;
            }

            if (existingItem != null)
                CheckOutFile(applicationObject.SourceControl, itemToAdd);

            var c = this.ParseFile(fileName);

            WriteToFile(showIncludeGuard, codeWriter, itemToAdd, c);

            // Add File to Project
            if (existingItem == null)
            {
                containingProject.ProjectItems.AddFromFile(itemToAdd);
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

        private static string GetItemFileName(string fileName)
        {
            string extension = Path.GetFileName(fileName).IsUpper() ? ".H" : ".h";

            return Path.Combine(Path.GetDirectoryName(fileName),
                Path.GetFileNameWithoutExtension(fileName) + extension);
        }

        private static void CheckOutFile(SourceControl sourceControl, string file)
        {
            if (sourceControl != null && sourceControl.IsItemUnderSCC(file)
                && !sourceControl.IsItemCheckedOut(file))
            {
                sourceControl.CheckOutItem(file);
            }
        }

        private static void WriteToFile(bool showIncludeGuard, CHeaderFileWriter codeWriter, string itemToAdd, CSourceFile c)
        {
            if (showIncludeGuard)
                codeWriter.IncludeGuard = new Regex(@"[^A-Z0-9_]").Replace(string.Format("__{0}__",
                    Path.GetFileName(itemToAdd).ToUpperInvariant()), "_");
            else
                codeWriter.IncludeGuard = null;

            using (var stream = File.Open(itemToAdd, FileMode.Create))
            {
                codeWriter.WriteHeaderFile(c, stream);
            }
        }

        private CSourceFile ParseFile(string fileName)
        {
            using (var stream = File.OpenRead(fileName))
            {
                return parserFactory.CreateParser(stream).PerformParse();
            }
        }

        private static ProjectItem FindExistingItem(string itemToAdd, Project containingProject)
        {
            string localFile = Path.GetFileName(itemToAdd);
            foreach (ProjectItem item in containingProject.ProjectItems)
            {
                if (item.Name.Equals(localFile, StringComparison.CurrentCultureIgnoreCase))
                    return item;
            }

            return null;
        }

        private static string SetHeaderComment(string template)
        {
            string headerComment = null;

            if (!string.IsNullOrEmpty(template))
            {
                headerComment = template.Replace("{Name}", GetUserName() ?? "{Name}")
                    .Replace("{Date}", DateTime.Now.ToString("dd-MMM-yyyy"))
                    .Replace("{Company}", GetCompanyName() ?? "{Company}");
            }

            return headerComment;
        }

        private static string GetCompanyName()
        {
            return ((AssemblyCompanyAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyCompanyAttribute), false))
                .Company;
        }

        private static string GetUserName()
        {
            return UserPrincipal.Current.DisplayName;
        }

        #endregion
    }
}
