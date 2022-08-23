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
        EnvelopeDetail[] envelopeDetail = null;
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
                //get batch level details               
                getBatchDetails(taskInfo);
                //get envelope level details
                getEnvelopeDetails(taskInfo);
                log.Debug("Completed setting the details of the batch");
                //now that we have the department...
                getDepartmentSpecifics();
                log.Debug("Pulled department specifics back from database");
                //and now determine and set the priority on the batch
                taskInfo.Task.Batch.Value().SetInt("Priority", BatchDetail.determineBatchPriority());
                log.Info("Batch priority of " + taskInfo.Task.Batch.Value().GetInt("Priority", 0).ToString() + " applied successfully");
                //set the batch values
                log.Debug("Preparing to apply departments and set initial batch values");
                setBatchValues(taskInfo);
                setEnvelopeValues(taskInfo);
                log.Debug("Finished applying departments and setting initial batch values");
                log.Debug("Preparing to process MDW Mappings");
                processMDWMappings(taskInfo);
                log.Debug("Finished mapping MDW values to Standard_MDF");                
                if (BatchDetail.InputSource.ToUpper() == "FAX" || BatchDetail.InputSource.ToUpper() == "DROPBOX")
                {
                    log.Debug("Preparing to send the batch no to the DB for FAX or DROPBOX");
                    sendBatchNo2DB(taskInfo);
                    log.Debug("Finished sending the batch no to the DB for FAX or DROPBOX");
                }
                log.Info("Completed the ExecuteTask method");
            }
            catch (Exception ex)
            {
                log.Error("Error within the ExecuteTask method: " + ex.Message, ex);
                throw ex;
            }
        }
        private void processMDWMappings(ITaskInformation taskInfo)
        {
            try
            {
                log.Debug("Preparing to call the getMDWMappings database call");
                BatchDetail.mdwMappings = _dbAccess.getMDWMappings();

                log.Debug("Looping through the envelopes in the batch and updating standard mdf based on the mappings");
                foreach (IBatchNode node in taskInfo.Task.TaskRoot.Children(3))
                {
                    foreach (IWorkflowStep wfStep in taskInfo.Task.Batch.WorkflowSteps)
                    {
                        if (wfStep.Name.ToUpper() == "STANDARD_MDF")
                        {
                            if (!object.ReferenceEquals(BatchDetail.mdwMappings, null))
                            {
                                foreach (KeyValuePair<string, string> content in BatchDetail.mdwMappings)
                                {
                                    string contentValue = string.Empty;
                                    foreach (IWorkflowStep wfStep2 in taskInfo.Task.Batch.WorkflowSteps)
                                    {
                                        if (wfStep2.Name.ToUpper() == "IMPORT")
                                        {
                                            contentValue = node.Values(wfStep2).GetString(content.Value, "");
                                            break;
                                        }
                                    }
                                    log.Debug("Setting Standard MDF variable " + content.Key + " to a value of " + contentValue);
                                    node.Values(wfStep).SetString(content.Key, contentValue);
                                }
                            }
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Error within the getMDWMappings method: " + ex.Message, ex);
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

                    BatchDetail.BatchPriority = dataRow["PRIORITY"].ToString();
                    BatchDetail.WorkCategory = dataRow["WORK_CATEGORY"].ToString();
                    BatchDetail.MDWMaxDocCount = dataRow["MDW_MAX_DOC_COUNT"].ToString();
                    BatchDetail.MaxClaimsPerDoc = dataRow["MAX_CLAIMS_PER_DOC"].ToString();
                    BatchDetail.BatchAgeing = dataRow["BATCH_AGING"].ToString();
                    BatchDetail.TrackUser = dataRow["TRACK_USER"].ToString();
                    BatchDetail.TrackPerformance = dataRow["TRACK_PERFORMANCE"].ToString();
                    BatchDetail.PriorityFactor = dataRow["PRIORITY_FACTOR"].ToString();
                    BatchDetail.DispatcherProject = dataRow["DISPATCHER_PROJECT"].ToString();
                    BatchDetail.DefaultAutoRejectPath = dataRow["DEFAULT_AUTO_REJECT_PATH"].ToString();
                    BatchDetail.DefaultRejectPath = dataRow["DEFAULT_REJECT_PATH"].ToString();
                    BatchDetail.DeletedBatchPath = dataRow["DELETED_BATCH_PATH"].ToString();
                    BatchDetail.DispCitrixPath = dataRow["DISP_CITRIX_PATH"].ToString();
                    BatchDetail.DispServerPath = dataRow["DISP_SERVER_PATH"].ToString();
                    BatchDetail.SkipAutoRotate = dataRow["SKIP_AUTOROTATE"].ToString();
                    BatchDetail.CreateMDWCover = dataRow["CREATE_MDW_COVER"].ToString();
                    BatchDetail.Direct2Workflow = dataRow["DIRECT_2_WORKFLOW"].ToString();
                }
            }
            catch (Exception ex)
            {
                log.Error("Error within the getDepartmentSpecifics method: " + ex.Message, ex);
                throw ex;
            }
        }
        private void getBatchDetails(ITaskInformation taskInfo)
        {
            try
            {
                log.Debug("Preparing to loop through workflow steps looking for STANDARD_MDF");
                foreach (IWorkflowStep wfStep in taskInfo.Task.Batch.WorkflowSteps)
                {
                    if (wfStep.Name.ToUpper() == "STANDARD_MDF")
                    {
                        BatchDetail.InputSource = taskInfo.Task.Batch.Tree.Values(wfStep).GetString("INPUT_SOURCE", "");
                        log.Debug("Entering switch statement looking for INPUT_SOURCE");
                        switch (BatchDetail.InputSource)
                        {
                            case "FAX":
                                #region CASE "FAX"
                                {
                                    log.Debug("Preparing to loop through workflow steps for an input source of FAX");
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
                            case "DROPBOX":
                                #region CASE "DROPBOX"
                                {
                                    log.Debug("Preparing to loop through workflow steps for an input source of DROPBOX");
                                    foreach (IWorkflowStep wfStep2 in taskInfo.Task.Batch.WorkflowSteps)
                                    {
                                        if (wfStep2.Name.ToUpper() == "IMPORT")
                                        {
                                            //pass the wfstep2 into a new method that will go out and pull all of the mdw configuration
                                            //settings and then assign them to stnd mdf items
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
                                    log.Debug("Completed setting batch details for an input source of DROPBOX");
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
                                taskInfo.Task.Batch.Tree.Values(wfStep).SetString("DEFAULTAUTOREJECTPATH", BatchDetail.DefaultAutoRejectPath);
                                taskInfo.Task.Batch.Tree.Values(wfStep).SetString("DEFAULTREJECTPATH", BatchDetail.DefaultRejectPath);
                                taskInfo.Task.Batch.Tree.Values(wfStep).SetString("DELETEDBATCHPATH", BatchDetail.DeletedBatchPath);
                                taskInfo.Task.Batch.Tree.Values(wfStep).SetString("DIRECT2WORKFLOW", BatchDetail.Direct2Workflow);
                                taskInfo.Task.Batch.Tree.Values(wfStep).SetString("DISP_CITRIX_PATH", BatchDetail.DispCitrixPath);
                                taskInfo.Task.Batch.Tree.Values(wfStep).SetString("DISP_SERVER_PATH", BatchDetail.DispServerPath);
                                taskInfo.Task.Batch.Tree.Values(wfStep).SetString("SKIP_AUTOROTATE", BatchDetail.SkipAutoRotate);
                                taskInfo.Task.Batch.Tree.Values(wfStep).SetString("CREATE_MDW_COVER", BatchDetail.CreateMDWCover);
                                break;
                            }
                    }
                }
                log.Debug("Completed looping through all workflow steps and setting batch values");
            }
            catch (Exception ex)
            {
                log.Error("Error within the SetBatchValues method: " + ex.Message, ex);
            }
        }
        private void getEnvelopeDetails(ITaskInformation taskInfo)
        {
            try
            {
                log.Debug("Preparing to loop through each envelope within the batch to get Envelope level values");
                envelopeDetail = new EnvelopeDetail[taskInfo.Task.TaskRoot.ChildCount(3)];
                int i = 0;
                foreach (IBatchNode node in taskInfo.Task.TaskRoot.Children(3))
                {
                    envelopeDetail[i] = new EnvelopeDetail();

                    switch (BatchDetail.InputSource)
                    {
                        case "EMAIL":
                            {
                                foreach (IWorkflowStep wfStep in taskInfo.Task.Batch.WorkflowSteps)
                                {
                                    switch (wfStep.Name.ToUpper())
                                    {
                                        case "EMAILIMPORT":
                                            {
                                                BatchDetail.PrepDate = node.Values(wfStep).GetString("StartDate", "");
                                                BatchDetail.PrepDate += " " + node.Values(wfStep).GetString("StartTime", "");
                                                BatchDetail.ReceivedDateCRD = BatchDetail.PrepDate;
                                                envelopeDetail[i].EmailSender = taskInfo.Task.Batch.Tree.Values(wfStep).GetString("EmailSenderAddr", "");
                                                envelopeDetail[i].EmailTo = taskInfo.Task.Batch.Tree.Values(wfStep).GetString("EmailAccount", "");
                                                envelopeDetail[i].EmailDate = node.Values(wfStep).GetString("EmailDate", "");
                                                envelopeDetail[i].EmailDate = Convert.ToDateTime(envelopeDetail[i].EmailDate).ToString("yyyy/MM/dd HH:mm:ss");
                                                BatchDetail.ReceivedDate = envelopeDetail[i].EmailDate;
                                                break;
                                            }
                                    }
                                }
                                break;
                            }

                    }
                    i++;
                }
            }
            catch (Exception ex)
            {
                log.Error("Error within the SetBatchDetails method: " + ex.Message, ex);
                throw ex;
            }
        }
        private void setEnvelopeValues(ITaskInformation taskInfo)
        {
            try
            {
                log.Debug("Preparing to loop through all workflow steps to set Envelope values");
                int i = 0;
                foreach (IBatchNode node in taskInfo.Task.TaskRoot.Children(3))
                {
                    foreach (IWorkflowStep wfStep in taskInfo.Task.Batch.WorkflowSteps)
                    {
                        switch (wfStep.Name.ToUpper())
                        {
                            case "STANDARD_MDF":
                                {
                                    node.Values(wfStep).SetString("E_DIRECT2WORKFLOW", BatchDetail.Direct2Workflow);
                                    node.Values(wfStep).SetString("E_EMAIL_SENDER", envelopeDetail[i].EmailSender);
                                    if (BatchDetail.InputSource == "EMAIL")
                                    {
                                        foreach (IBatchNode dnode in node.Children(1))
                                        {
                                            dnode.Values(wfStep).SetString("D_EMAIL_SENDER", envelopeDetail[i].EmailSender);
                                            dnode.Values(wfStep).SetString("D_EMAIL_TO", envelopeDetail[i].EmailTo);
                                            dnode.Values(wfStep).SetString("D_EMAIL_DATE", envelopeDetail[i].EmailDate);
                                        }
                                    }
                                    
                                    break;
                                }
                        }
                    }
                    i++;
                }
                log.Debug("Completed looping through all workflow steps and setting envelope values");
            }
            catch (Exception ex)
            {
                log.Error("Error within the SetBatchValues method: " + ex.Message, ex);
            }
        }
        private void sendBatchNo2DB(ITaskInformation taskInfo)
        {
            //now loop through all steps to get to standard mdf
            foreach (IWorkflowStep wfStep in taskInfo.Task.Batch.WorkflowSteps)
            {
                if (wfStep.Name.ToUpper() == "STANDARD_MDF")
                {
                    //we need to loop through each document in the batch 
                    //and update the db with the batch it entered into
                    foreach (IBatchNode node in taskInfo.Task.TaskRoot.Children(1))
                    {
                        //based on what the input source is we have to do different things
                        switch (BatchDetail.InputSource)
                        {
                            case "FAX":
                                {
                                    //go to the db and update using fax key and fax id
                                    DocumentDetail.FaxID = node.Values(wfStep).GetString("D_FAX_ID", "");
                                    DocumentDetail.FaxKey = node.Values(wfStep).GetString("D_FAX_KEY", "");
                                    log.Debug("Preparing to update the db for fax id " + DocumentDetail.FaxID +
                                        " with a fax key of " + DocumentDetail.FaxKey + " indicating it was placed" +
                                        " in batch " + BatchDetail.BatchNo);
                                    //now call the update
                                    _dbAccess.updateFaxXref();
                                    break;
                                }
                            case "DROPBOX":
                                {
                                    //go to the db and update using the guid
                                    DocumentDetail.dGUID = node.Values(wfStep).GetString("D_GUID", "");
                                    log.Debug("Preparing to update the db for dropbox GUID " + DocumentDetail.dGUID +
                                        " indicating it was placed in batch " + BatchDetail.BatchNo);
                                    //now call the update
                                    _dbAccess.updateImportXref();
                                    break;
                                }
                        }
                    }
                }
            }
        }
    }
}
