using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Emc.InputAccel.QuickModule.ClientScriptingInterface;
using Emc.InputAccel.ScriptEngine.Scripting;

namespace CNO.BPA.MDWAudit
{
    public class TaskEvents : ITaskEvents
    {
        DataHandler.DataAccess _dbAccess = null;

        [CustomParameterType(typeof(CustomParameters))]
        public void ExecuteTask(ITaskInformation taskInfo)
        {
            CustomParameters parmsCustom;
            parmsCustom = (CustomParameters)taskInfo.CustomParameter.Value;
            //generate the batch number
            _dbAccess = new DataHandler.DataAccess(ref parmsCustom);
            BatchDetail.ScannerID = _dbAccess.getScannerID();
            BatchDetail.BatchNo = _dbAccess.getBatchNo();
            //assign the batch name to the batch
            taskInfo.Task.Batch.Value().SetString("BatchName", BatchDetail.BatchNo);


            foreach (IWorkflowStep wfStep in taskInfo.Task.Batch.WorkflowSteps)
            {
                if (wfStep.Name.ToUpper() == "IMPORT")
                {
                    foreach (IBatchNode node in taskInfo.Task.TaskRoot.Children(1))
                    {                      
                        BatchDetail.FaxID = node.Values(wfStep).GetString("XMLValue0_Level3", "");
                        BatchDetail.FaxKey = node.Values(wfStep).GetString("XMLValue4_Level3", "");
                        //if this was a fax import update the fax xref table
                        if (BatchDetail.FaxID.Length > 0)
                        {
                            _dbAccess.updateFaxXref();
                        }
                        
                    }
                    break;
                }
            }
            //we need to pull back the department and other data from the import step
            foreach (IWorkflowStep wfStep in taskInfo.Task.Batch.WorkflowSteps)
            {
                if (wfStep.Name.ToUpper() == "IMPORT")
                {
                    BatchDetail.Department = taskInfo.Task.Batch.Tree.Values(wfStep).GetString("SourceIndicator", "");
                    //BatchDetail.FaxID = taskInfo.Task.Batch.Tree.Values(wfStep).GetString("XMLValue0_Level3", "");
                    //BatchDetail.FaxKey = taskInfo.Task.Batch.Tree.Values(wfStep).GetString("XMLValue4_Level3", "");
                    BatchDetail.PrepDate = taskInfo.Task.Batch.Tree.Values(wfStep).GetString("CreateDate", "");
                    BatchDetail.PrepDate += " " + taskInfo.Task.Batch.Tree.Values(wfStep).GetString("CreateTime", "");
                    BatchDetail.ReceivedDate = BatchDetail.PrepDate;
                    BatchDetail.ReceivedDateCRD = BatchDetail.PrepDate;
                    //we need to pull the site id out of the department name
                    BatchDetail.SiteID = BatchDetail.Department.Substring(0, 3);
                    break;
                }
            }
            //now that we have the department...
            getDepartmentSpecifics();
            //and now determine and set the priority on the batch
            taskInfo.Task.Batch.Value().SetInt("Priority", BatchDetail.determineBatchPriority());


            //setup all 3 manual touchpoints with the department selection and store the batch details
            foreach (IWorkflowStep wfStep in taskInfo.Task.Batch.WorkflowSteps)
            {
                switch (wfStep.Name.ToUpper())
                {
                    case "AUTOCLASSIFY":
                        {
                            taskInfo.Task.Batch.Value().SetString("$batch=" +
                            taskInfo.Task.Batch.Id + "/AutoClassify.IADepartments", BatchDetail.Department);
                            break;
                        }
                    case "AUTOVALIDATION":
                        {
                            taskInfo.Task.Batch.Value().SetString("$batch=" +
                                taskInfo.Task.Batch.Id + "/AutoValidation.IADepartments", BatchDetail.Department);
                            break;
                        }
                    case "MANUALCLASSIFY":
                        {
                            taskInfo.Task.Batch.Value().SetString("$batch=" +
                                taskInfo.Task.Batch.Id + "/ManualClassify.IADepartments", BatchDetail.Department);
                            break;
                        }
                    case "MANUALVAL":
                        {
                            taskInfo.Task.Batch.Value().SetString("$batch=" +
                                taskInfo.Task.Batch.Id + "/ManualVal.IADepartments", BatchDetail.Department);
                            break;
                        }
                    case "MANUALINDEX":
                        {
                            taskInfo.Task.Batch.Value().SetString("$batch=" +
                                taskInfo.Task.Batch.Id + "/ManualIndex.IADepartments", BatchDetail.Department);
                            break;
                        }
                    case "STANDARD_MDF":
                        {
                            taskInfo.Task.Batch.Tree.Values(wfStep).SetString("BATCH_NO", BatchDetail.BatchNo);
                            taskInfo.Task.Batch.Tree.Values(wfStep).SetString("BATCH_DEPARTMENT", BatchDetail.Department);
                            taskInfo.Task.Batch.Tree.Values(wfStep).SetString("BATCH_PRIORITY", BatchDetail.BatchPriority);
                            taskInfo.Task.Batch.Tree.Values(wfStep).SetString("SITE_ID", BatchDetail.SiteID);
                            taskInfo.Task.Batch.Tree.Values(wfStep).SetString("WORK_CATEGORY", BatchDetail.WorkCategory);
                            taskInfo.Task.Batch.Tree.Values(wfStep).SetString("BOX_NO", BatchDetail.BoxNo);
                            taskInfo.Task.Batch.Tree.Values(wfStep).SetString("PREP_OPERATOR", BatchDetail.PrepOperator);
                            taskInfo.Task.Batch.Tree.Values(wfStep).SetString("PREP_DATE", BatchDetail.PrepDate);
                            taskInfo.Task.Batch.Tree.Values(wfStep).SetString("SCANNER_ID", BatchDetail.ScannerID);
                            taskInfo.Task.Batch.Tree.Values(wfStep).SetString("RECEIVED_DATE", BatchDetail.ReceivedDate);
                            taskInfo.Task.Batch.Tree.Values(wfStep).SetString("CRD_RECEIVED_DATE", BatchDetail.ReceivedDateCRD);
                            taskInfo.Task.Batch.Tree.Values(wfStep).SetString("BATCH_AGEING", BatchDetail.BatchAgeing);
                            taskInfo.Task.Batch.Tree.Values(wfStep).SetString("MAX_CLAIMS_PER_DOC", BatchDetail.MaxClaimsPerDoc);
                            taskInfo.Task.Batch.Tree.Values(wfStep).SetString("TRACK_USER", BatchDetail.TrackUser);
                            taskInfo.Task.Batch.Tree.Values(wfStep).SetString("TRACK_PERFORMANCE", BatchDetail.TrackPerformance);
                            break;
                        }
                }
            }

        }
        private void getDepartmentSpecifics()
        {
            DataSet datasetResults = _dbAccess.getDepartmentDetails();
            DataRow dataRow = datasetResults.Tables[0].Rows[0];

            BatchDetail.BatchPriority = dataRow.ItemArray.GetValue(1).ToString();   //priority
            BatchDetail.WorkCategory = dataRow.ItemArray.GetValue(2).ToString();   //work category         
            BatchDetail.MDWMaxDocCount = dataRow.ItemArray.GetValue(3).ToString();   //mdw max doc count    
            BatchDetail.MaxClaimsPerDoc = dataRow.ItemArray.GetValue(4).ToString();   //max claims per doc
            BatchDetail.BatchAgeing = dataRow.ItemArray.GetValue(5).ToString();   //batch ageing
            BatchDetail.TrackUser = dataRow.ItemArray.GetValue(6).ToString();   //track user
            BatchDetail.TrackPerformance = dataRow.ItemArray.GetValue(7).ToString();   //track performance
            BatchDetail.PriorityFactor = dataRow.ItemArray.GetValue(8).ToString();   //priority factor

            //
        }
    }
}
