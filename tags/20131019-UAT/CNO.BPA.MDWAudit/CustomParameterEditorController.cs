using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emc.InputAccel.QuickModule.ClientScriptingInterface;

namespace CNO.BPA.MDWAudit
{
    public class CustomParameterEditorController : ICustomParameterEditor
    {
        public object EditParameter(System.Windows.Forms.IWin32Window parentWindow, IWorkflowStep step,
            object objectToEdit)
        {
            //first setup and grab the custom parameters
            CustomParameters parmsCustom;
            parmsCustom = (CustomParameters)objectToEdit;
            //now create and pass the custom parms to the editor
            CustomParameterEditor1 parmsEditor = new CustomParameterEditor1(ref parmsCustom, step.BatchProcess.Name);
            //display the dialog
            parmsEditor.ShowDialog();
            //return the updated custom parameters
            return parmsCustom;
        }
    }
}

