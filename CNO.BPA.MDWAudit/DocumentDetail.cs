using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
//using Emc.InputAccel.QuickModule.ClientScriptingInterface;

namespace CNO.BPA.MDWAudit
{
    public static class DocumentDetail
    {
        #region Variables

        public static string FaxID = string.Empty;
        public static string FaxKey = string.Empty;
        public static string dGUID = string.Empty;
        #endregion

        public static void Clear()
        {
            FaxID = string.Empty;
            FaxKey = string.Empty;
            dGUID = string.Empty;
        }
    }
}
