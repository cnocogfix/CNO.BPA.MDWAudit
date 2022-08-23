using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Emc.InputAccel.QuickModule.ClientScriptingInterface;
using Emc.InputAccel.ScriptEngine.Scripting;
using log4net;
using System.IO;
using System.Reflection;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace CNO.BPA.MDWAudit
{
    public class TaskEvents : ITaskEvents
    {
        DataHandler.DataAccess _dbAccess = null;
        private log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType); 

        [CustomParameterType(typeof(CustomParameters))]
        public void ExecuteTask(ITaskInformation taskInfo)
        {
            try
            {
                //initialize the logger
                FileInfo fi = new FileInfo(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CNO.BPA.MDWAudit.dll.config"));
                log4net.Config.XmlConfigurator.Configure(fi);   
                //initLogging();
                log.Info("Beginning the ExecuteTask method");
                CustomParameters parmsCustom;
                parmsCustom = (CustomParameters)taskInfo.CustomParameter.Value;
                log.Debug("Pulled the custom parameters from the process into a local object");                
                //generate the batch number
                log.Debug("Establishing the database connection");
                _dbAccess = new DataHandler.DataAccess(ref parmsCustom);
                log.Debug("Calling the procedure to return the scanner id");
                BatchDetail.ScannerID = _dbAccess.getScannerID();
                log.Debug("Calling the procedure to generate a batch number");
                BatchDetail.BatchNo = _dbAccess.getBatchNo();
                log.Info("Generated batch number: " + BatchDetail.BatchNo);
                //assign the batch name to the batch
                taskInfo.Task.Batch.Value().SetString("BatchName", BatchDetail.BatchNo);
                log.Info("Applied the new batch number to the batch");
                //set the batch details
                setBatchDetails(taskInfo);
                log.Debug("Completed setting the details of the batch");
                //now that we have the department...
                getDepartmentSpecifics();
                log.Debug("Pulled department specifics back from database");
                //and now determine and set the priority on the batch
                taskInfo.Task.Batch.Value().SetInt("Priority", BatchDetail.determineBatchPriority());
                log.Info("Batch priority of " + taskInfo.Task.Batch.Value().GetInt("Priority", 0).ToString() + " applied successfully");
                //set the batch values
                setBatchValues(taskInfo);
                log.Debug("Finished applying departments and setting initial batch values");
                log.Info("Completed the ExecuteTask method");
            }
            catch (Exception ex)
            {
                log.Error("Error within the ExecuteTask method: " + ex.Message, ex);
                throw ex;
            }
        }
        private void getDepartmentSpecifics()
        {
            try
            {
                log.Debug("Preparing to call the getDepartmentDetails database call");
                DataSet datasetResults = _dbAccess.getDepartmentDetails();

                if (!object.ReferenceEquals(datasetResults, null))
                {
                    DataRow dataRow = datasetResults.Tables[0].Rows[0];

                    BatchDetail.BatchPriority = dataRow.ItemArray.GetValue(1).ToString();   //priority
                    BatchDetail.WorkCategory = dataRow.ItemArray.GetValue(2).ToString();   //work category         
                    BatchDetail.MDWMaxDocCount = dataRow.ItemArray.GetValue(3).ToString();   //mdw max doc count    
                    BatchDetail.MaxClaimsPerDoc = dataRow.ItemArray.GetValue(4).ToString();   //max claims per doc
                    BatchDetail.BatchAgeing = dataRow.ItemArray.GetValue(5).ToString();   //batch ageing
                    BatchDetail.TrackUser = dataRow.ItemArray.GetValue(6).ToString();   //track user
                    BatchDetail.TrackPerformance = dataRow.ItemArray.GetValue(7).ToString();   //track performance
                    BatchDetail.PriorityFactor = dataRow.ItemArray.GetValue(8).ToString();   //priority factor
                }
            }
            catch (Exception ex)
            {
                log.Error("Error within the getDepartmentSpecifics method: " + ex.Message, ex);
                throw ex;
            }            
        }
        private void setBatchDetails(ITaskInformation taskInfo)
        {
            try
            {
                log.Debug("Preparing to loop through workflow steps looking for STANDARD_MDF");
                foreach (IWorkflowStep wfStep in taskInfo.Task.Batch.WorkflowSteps)
                {
                    if (wfStep.Name.ToUpper() == "STANDARD_MDF")
                    {
                        log.Debug("Entering switch statement looking for INPUT_SOURCE");
                        switch (taskInfo.Task.Batch.Tree.Values(wfStep).GetString("INPUT_SOURCE", ""))
                        {
                            case "FAX":
                                #region CASE "FAX"
                                {
                                    log.Debug("Preparing to loop through workflow steps for an input source of FAX");
                                    foreach (IWorkflowStep wfStep2 in taskInfo.Task.Batch.WorkflowSteps)
                                    {

                                        if (wfStep2.Name.ToUpper() == "IMPORT")
                                        {
                                            foreach (IBatchNode node in taskInfo.Task.TaskRoot.Children(1))
                                            {
                                                BatchDetail.FaxID = node.Values(wfStep2).GetString("XMLValue0_Level3", "");
                                                BatchDetail.FaxKey = node.Values(wfStep2).GetString("XMLValue4_Level3", "");
                                                //if this was a fax import update the fax xref table
                                                if (BatchDetail.FaxID.Length > 0)
                                                {
                                                    _dbAccess.updateFaxXref();
                                                }
                                            }
                                            BatchDetail.Department = taskInfo.Task.Batch.Tree.Values(wfStep2).GetString("SourceIndicator", "");
                                            BatchDetail.PrepDate = taskInfo.Task.Batch.Tree.Values(wfStep2).GetString("CreateDate", "");
                                            BatchDetail.PrepDate += " " + taskInfo.Task.Batch.Tree.Values(wfStep2).GetString("CreateTime", "");
                                            BatchDetail.ReceivedDate = BatchDetail.PrepDate;
                                            BatchDetail.ReceivedDateCRD = BatchDetail.PrepDate;
                                            //we need to pull the site id out of the department name
                                            if (BatchDetail.Department.Length > 0)
                                            {
                                                BatchDetail.SiteID = BatchDetail.Department.Substring(0, 3);
                                            }
                                            break;
                                        }
                                    }
                                    log.Debug("Completed setting batch details for an input source of FAX");
                                    break;
                                }
                                #endregion
                            case "EMAIL":
                                #region CASE "EMAIL"
                                {
                                    log.Debug("Preparing to loop through workflow steps for an input source of EMAIL");
                                    //we need to pull back the department and other data from the import step
                                    foreach (IWorkflowStep wfStep2 in taskInfo.Task.Batch.WorkflowSteps)
                                    {
                                        if (wfStep2.Name.ToUpper() == "EMAILIMPORT")
                                        {
                                            BatchDetail.PrepDate = taskInfo.Task.Batch.Tree.Values(wfStep2).GetString("StartDate", "");
                                            BatchDetail.PrepDate += " " + taskInfo.Task.Batch.Tree.Values(wfStep2).GetString("StartTime", "");
                                            BatchDetail.ReceivedDate = BatchDetail.PrepDate;
                                            BatchDetail.ReceivedDateCRD = BatchDetail.PrepDate;
                                        }
                                        if (wfStep2.Name.ToUpper() == "STANDARD_MDF")
                                        {
                                            BatchDetail.Department = taskInfo.Task.Batch.Tree.Values(wfStep).GetString("BATCH_DEPARTMENT", "");
                                            //we need to pull the site id out of the department name
                                            if (BatchDetail.Department.Length > 0)
                                            {
                                                BatchDetail.SiteID = BatchDetail.Department.Substring(0, 3);
                                            }
                                        }
                                    }
                                    log.Debug("Completed setting batch details for an input source of EMAIL");
                                    break;
                                }
                                #endregion
                            case "MDW":
                                #region CASE "MDW"
                                {
                                    log.Debug("Preparing to loop through workflow steps for an input source of MDW");
                                    foreach (IWorkflowStep wfStep2 in taskInfo.Task.Batch.WorkflowSteps)
                                    {
                                        if (wfStep2.Name.ToUpper() == "IMPORT")
                                        {
                                            BatchDetail.Department = taskInfo.Task.Batch.Tree.Values(wfStep2).GetString("SourceIndicator", "");
                                            BatchDetail.PrepDate = taskInfo.Task.Batch.Tree.Values(wfStep2).GetString("CreateDate", "");
                                            BatchDetail.PrepDate += " " + taskInfo.Task.Batch.Tree.Values(wfStep2).GetString("CreateTime", "");
                                            BatchDetail.ReceivedDate = BatchDetail.PrepDate;
                                            BatchDetail.ReceivedDateCRD = BatchDetail.PrepDate;
                                            //we need to pull the site id out of the department name
                                            if (BatchDetail.Department.Length > 0)
                                            {
                                                BatchDetail.SiteID = BatchDetail.Department.Substring(0, 3);
                                            }
                                            break;
                                        }
                                    }
                                    log.Debug("Completed setting batch details for an input source of MDW");
                                    break;
                                }
                                #endregion
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Error within the SetBatchDetails method: " + ex.Message, ex);
                throw ex;
            }
        }
        private void setBatchValues(ITaskInformation taskInfo)
        {
            try
            {
                log.Debug("Preparing to loop through all workflow steps to set batch values");
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
                log.Debug ("Completed looping through all workflow steps and setting batch values");
            }
            catch(Exception ex)
            {
                log.Error("Error within the SetBatchValues method: " + ex.Message, ex);
            }
        }
    }
}
