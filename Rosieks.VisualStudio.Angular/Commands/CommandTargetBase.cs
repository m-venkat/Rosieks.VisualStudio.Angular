﻿using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Rosieks.VisualStudio.Angular.Commands
{
    internal abstract class CommandTargetBase<TCommandEnum> : IOleCommandTarget where TCommandEnum : struct, IComparable
    {
        private IOleCommandTarget _nextCommandTarget;
        protected readonly IWpfTextView TextView;

        public Guid CommandGroup { get; set; }

        public ReadOnlyCollection<uint> CommandIds { get; private set; }

        public CommandTargetBase(IVsTextView adapter, IWpfTextView textView, params TCommandEnum[] commandIds)
            : this(adapter, textView, typeof(TCommandEnum).GUID, Array.ConvertAll(commandIds, e => Convert.ToUInt32(e, CultureInfo.InvariantCulture)))
        { }
        public CommandTargetBase(IVsTextView adapter, IWpfTextView textView, Guid commandGroup, params uint[] commandIds)
        {
            CommandGroup = commandGroup;
            CommandIds = new ReadOnlyCollection<uint>(commandIds);
            TextView = textView;

            Dispatcher.CurrentDispatcher.InvokeAsync(() =>
            {
                // Add the target later to make sure it makes it in before other command handlers
                ErrorHandler.ThrowOnFailure(adapter.AddCommandFilter(this, out _nextCommandTarget));
            }, DispatcherPriority.ApplicationIdle);
        }

        protected virtual bool IsEnabled()
        {
            return true;
        }

        protected abstract bool Execute(TCommandEnum commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut);

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (pguidCmdGroup == CommandGroup && CommandIds.Contains(nCmdID) && this.IsEnabled())
            {
                bool result = Execute((TCommandEnum)(object)(int)nCmdID, nCmdexecopt, pvaIn, pvaOut);

                if (result)
                {
                    return VSConstants.S_OK;
                }
            }

            return _nextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (pguidCmdGroup != CommandGroup)
            {
                return _nextCommandTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
            }

            for (int i = 0; i < cCmds; i++)
            {
                if (CommandIds.Contains(prgCmds[i].cmdID))
                {
                    if (IsEnabled())
                    {
                        prgCmds[i].cmdf = (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED);
                        return VSConstants.S_OK;
                    }

                    prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_SUPPORTED;
                }
            }

            return _nextCommandTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }
    }
}
