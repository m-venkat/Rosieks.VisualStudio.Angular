﻿using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Rosieks.VisualStudio.Angular.Extensions;
using Rosieks.VisualStudio.Angular.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rosieks.VisualStudio.Angular.Extensions;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Shell;

namespace Rosieks.VisualStudio.Angular.Commands
{
    internal class ControllerGoToDefinition : CommandTargetBase<VSConstants.VSStd97CmdID>
    {
        private readonly DTE dte;
        private readonly IVsTextView adapter;
        private readonly IStandardClassificationService standardClassifications;

        public ControllerGoToDefinition(IVsTextView adapter, IWpfTextView textView, DTE dte, IStandardClassificationService standardClassifications) : base(adapter, textView, VSConstants.VSStd97CmdID.GotoDefn)
        {
            this.adapter = adapter;
            this.dte = dte;
            this.standardClassifications = standardClassifications;
        }

        protected override bool IsEnabled()
        {
            return this.dte.ActiveDocument.Name.EndsWith(".js");
        }

        protected override bool Execute(VSConstants.VSStd97CmdID commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            string controllerName = this.TextView.GetJavaScriptStringValue(this.standardClassifications);
            if (!string.IsNullOrEmpty(controllerName))
            {
                var currentDocumentPath = ServiceProvider.GlobalProvider.GetCurrentDocumentPath();
                var ngHierarchy = NgHierarchyFactory.Find(currentDocumentPath);
                var controllerMetadata = ngHierarchy.Controllers.Value.FirstOrDefault(x => x.Name == controllerName);
                if (controllerMetadata != null)
                {
                    this.dte.OpenFileInPreviewTab(controllerMetadata.Path);

                    return true;
                }
                else
                {
                    this.dte.StatusBar.Text = $"Cannot find controller {controllerName}";

                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }
}
