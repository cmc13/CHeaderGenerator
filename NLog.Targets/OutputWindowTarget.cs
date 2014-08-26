using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.DataAnnotations;

namespace NLog.Targets.Custom
{
    [Target("OutputWindow")]
    public sealed class OutputWindowTarget : TargetWithLayout
    {
        private string paneName;
        private OutputWindowPane pane = null;

        [Required]
        public string PaneName
        {
            get { return this.paneName; }
            set
            {
                if (this.paneName != value && !string.IsNullOrWhiteSpace(value))
                {
                    this.paneName = value;

                    var applicationObject = (DTE2)Package.GetGlobalService(typeof(SDTE));
                    var outputWindow = applicationObject.ToolWindows.OutputWindow;
                    this.pane = outputWindow.OutputWindowPanes.Add(this.PaneName);
                }
            }
        }

        protected override void Write(LogEventInfo logEvent)
        {
            if (this.pane != null)
            {
                var logMessage = this.Layout.Render(logEvent);
                this.pane.OutputString(logMessage);
                this.pane.OutputString(Environment.NewLine);
            }
        }
    }
}
