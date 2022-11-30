using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;

namespace SpeedMeter
{
    [Export(typeof(IVsTextViewCreationListener))]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    [ContentType("text")]
    internal class InputFilter : IVsTextViewCreationListener
    {
        [Import]
        internal IVsEditorAdaptersFactoryService AdapterService = null;

        internal SpeedAdornment adornment;

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            ITextView textView = AdapterService.GetWpfTextView(textViewAdapter);
            if (textView == null)
                return;

            adornment = textView.Properties.GetProperty<SpeedAdornment>(typeof(SpeedAdornment));

            textView.Properties.GetOrCreateSingletonProperty(() => new CharFilter(textViewAdapter, textView, adornment));
        }
    }

    internal sealed class CharFilter : IOleCommandTarget
    {
        IOleCommandTarget nextCommandHandler;
        SpeedAdornment adornment;
        ITextView textView;

        internal CharFilter(IVsTextView adapter, ITextView textView, SpeedAdornment adornment)
        {
            this.textView = textView;
            this.adornment = adornment;

            adapter.AddCommandFilter(this, out nextCommandHandler);
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            int hr = VSConstants.S_OK;

            char typedChar;
            if (TryGetTypedChar(pguidCmdGroup, nCmdID, pvaIn, out typedChar))
            {
                adornment.UpdateValues(typedChar);
            }

            hr = nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

            return hr;
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            return nextCommandHandler.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        bool TryGetTypedChar(Guid cmdGroup, uint nCmdID, IntPtr pvaIn, out char typedChar)
        {
            typedChar = char.MinValue;

            if (cmdGroup != VSConstants.VSStd2K || nCmdID != (uint)VSConstants.VSStd2KCmdID.TYPECHAR)
                return false;

            typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
            return true;
        }
    }
}
