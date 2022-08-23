using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using log4net;
using System.IO;
using System.Reflection;
using Emc.InputAccel.CaptureClient;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace CNO.BPA.MDWAudit
{
    public class MDWAuditModule : CustomCodeModule
    {
        
        DataHandler.DataAccess _dbAccess = null;
        EnvelopeDetail[] envelopeDetail = null;
        private log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        public MDWAuditModule()
        {

        }

        public override void ExecuteTask(IClientTask task, IBatchContext batchContext)
        {
            
            try
            {

                //initialize the logger
                FileInfo fi = new FileInfo(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CNO.BPA.MDWAudit.dll.config"));
                log4net.Config.XmlConfigurator.Configure(fi);
                //initLogging();
                log.Info("Beginning the ExecuteTask method");
                CustomParameters parmsCustom = new CustomParameters();
                parmsCustom.DSN = task.BatchNode.StepData.StepConfiguration.ReadString("_DSN", "");
                parmsCustom.UserName = task.BatchNode.StepData.StepConfiguration.ReadString("_USERNAME", "");
                parmsCustom.Password = task.BatchNode.StepData.StepConfiguration.ReadString("_PASSWORD", "");
                log.Debug("Pulled the custom parameters from the process into a local object");
                //generate the batch number
                log.Debug("Establishing the database connection");
                _dbAccess = new DataHandler.DataAccess(ref parmsCustom);
                log.Debug("Calling the procedure to return the scanner id");
                BatchDetail.ScannerID = _dbAccess.getScannerID();
                log.Debug("Calling the procedure to generate a batch number");
                
                //Nick Addition
                if (batchContext.GetRoot("Standard_MDF").NodeData.ValueSet.ReadString("SCAN_TYPE") == "RESCAN")
                {
                    string receivedDate = batchContext.GetRoot("Standard_MDF").NodeData.ValueSet.ReadString("RECEIVED_DATE");
                    BatchDetail.BatchNo = _dbAccess.getBatchNo(receivedDate);
                }
                else{
                    BatchDetail.BatchNo = _dbAccess.getBatchNo();  //this was not in an if-statement before
                }

                //Nick Addition
                log.Info("Generated batch number: " + BatchDetail.BatchNo);
                //assign the batch name to the batch
                batchContext.BatchName = BatchDetail.BatchNo;
                log.Info("Applied the new batch number to the batch");
                //get batch level details               
                getBatchDetails(task, batchContext);
                //get envelope level details
                getEnvelopeDetails(task, batchContext);
                log.Debug("Completed setting the details of the batch");
                //now that we have the department...
                getDepartmentSpecifics();
                log.Debug("Pulled department specifics back from database");
                //and now determine and set the priority on the batch
                batchContext.WriteBatchProperty("Priority", BatchDetail.determineBatchPriority());
                log.Info("Batch priority of " + batchContext.ReadBatchProperty("Priority").ToString() + " applied successfully");
                //set the batch values
                log.Debug("Preparing to apply departments and set initial batch values");
                setBatchValues(task, batchContext);
                setEnvelopeValues(task, batchContext);
                log.Debug("Finished applying departments and setting initial batch values");
                log.Debug("Preparing to process MDW Mappings");
                processMDWMappings(task, batchContext);
                log.Debug("Finished mapping MDW values to Standard_MDF");                
                if (BatchDetail.InputSource.ToUpper() == "FAX" || BatchDetail.InputSource.ToUpper() == "DROPBOX")
                {
                    log.Debug("Preparing to send the batch no to the DB for FAX or DROPBOX");
                    sendBatchNo2DB(task, batchContext);
                    log.Debug("Finished sending the batch no to the DB for FAX or DROPBOX");
                }
                log.Info("Completed the ExecuteTask method");
                task.CompleteTask();
            }
            catch (Exception ex)
            {
                log.Error("Error within the ExecuteTask method: " + ex.Message, ex);
                task.FailTask(FailTaskReasonCode.GenericUnrecoverableError, ex);
                //throw ex;
            }

        }

        public override void StartModule(ICodeModuleStartInfo startInfo)
        {
            startInfo.ShowStatusMessage("Try2");
        }

        
        public override Boolean SetupCodeModule(System.Windows.Forms.Control parentWindow, IValueAccessor stepConfiguration)
        {
            
            CustomParameters parmsCustom = new CustomParameters();
            parmsCustom.DSN = stepConfiguration.ReadString("_DSN", "");
            parmsCustom.UserName = stepConfiguration.ReadString("_USERNAME", "");
            parmsCustom.Password = stepConfiguration.ReadString("_PASSWORD", "");

            CustomParameterEditor1 myDialog = new CustomParameterEditor1(parmsCustom);

            myDialog.ShowDialog(parentWindow);

            if (myDialog.DialogResult == System.Windows.Forms.DialogResult.OK)
            {
                stepConfiguration.WriteString("_DSN", parmsCustom.DSN);
                stepConfiguration.WriteString("_USERNAME", parmsCustom.UserName);
                stepConfiguration.WriteString("_PASSWORD", parmsCustom.Password);
                return true;
            }
            return false;

        }

        
        private void processMDWMappings(IClientTask task, IBatchContext batchContext)
        {
            try
            {
                log.Debug("Preparing to call the getMDWMappings database call");
                BatchDetail.mdwMappings = _dbAccess.getMDWMappings();

                log.Debug("Looping through the envelopes in the batch and updating standard mdf based on the mappings");
                IBatchNode currentNode = task.BatchNode;



                foreach (IBatchNode env in currentNode.GetDescendantNodes(3))
                {
                    IBatchNode standardMDFStepEnvNode = batchContext.GetStepNode(env, "STANDARD_MDF");

                    if (!object.ReferenceEquals(BatchDetail.mdwMappings, null))
                    {
                        foreach (KeyValuePair<string, string> content in BatchDetail.mdwMappings)
                        {
                            string contentValue = string.Empty;
                            IBatchNode importStepNode = batchContext.GetStepNode(env, "IMPORT");

                            contentValue = importStepNode.NodeData.ValueSet.ReadString(content.Value, "");

                            log.Debug("Setting Standard MDF variable " + content.Key + " to a value of " + contentValue);
                            standardMDFStepEnvNode.NodeData.ValueSet.WriteString(content.Key, contentValue);

                            foreach (IBatchNode doc in standardMDFStepEnvNode.GetDescendantNodes(1))
                            {
                                doc.NodeData.ValueSet.WriteString(content.Key, contentValue);
                            }
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

                    //START:AR122352
                    BatchDetail.GenerateXLS = dataRow["GENERATE_XLS"].ToString();
                    BatchDetail.DefaultXLSPath = dataRow["DEFAULT_XLS_PATH"].ToString();
                    //END:AR122352
                }
            }
            catch (Exception ex)
            {
                log.Error("Error within the getDepartmentSpecifics method: " + ex.Message, ex);
                throw ex;
            }
        }

        
        private void getBatchDetails(IClientTask task, IBatchContext batchContext)
        {
            try
            {
                log.Debug("Preparing to loop through workflow steps looking for STANDARD_MDF");
                IBatchNode standardMDFStepNode =  batchContext.GetStepNode(task.BatchNode, "STANDARD_MDF");
                BatchDetail.InputSource = standardMDFStepNode.NodeData.ValueSet.ReadString("INPUT_SOURCE", "");
                // BatchDetail.InputSource = taskInfo.Task.Batch.Tree.Values(wfStep).GetString("INPUT_SOURCE", "");
                log.Debug("Entering switch statement looking for INPUT_SOURCE");
                switch (BatchDetail.InputSource)
                {
                    case "FAX":
                        #region CASE "FAX"
                        {
                            log.Debug("Preparing to loop through workflow steps for an input source of FAX");
                            IBatchNode importStepNode = batchContext.GetStepNode(task.BatchNode, "IMPORT");
                            //Get page level node
                            importStepNode = importStepNode.GetDescendantNodes(0)[0];

                            BatchDetail.Department = importStepNode.NodeData.ValueSet.ReadString("SourceIndicator", "");
                            BatchDetail.PrepDate = importStepNode.NodeData.ValueSet.ReadString("CreateDate", "");
                            BatchDetail.PrepDate += " " + importStepNode.NodeData.ValueSet.ReadString("CreateTime", "");
                            BatchDetail.ReceivedDate = BatchDetail.PrepDate;
                            BatchDetail.ReceivedDateCRD = BatchDetail.PrepDate;
                            if (BatchDetail.Department.Length > 0)
                            {
                                BatchDetail.SiteID = BatchDetail.Department.Substring(0, 3);
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

                            BatchDetail.Department = standardMDFStepNode.NodeData.ValueSet.ReadString("BATCH_DEPARTMENT", "");
                            if (BatchDetail.Department.Length > 0)
                            {
                                BatchDetail.SiteID = BatchDetail.Department.Substring(0, 3);
                            }



                            log.Debug("Completed setting batch details for an input source of EMAIL");
                            break;
                        }
                        #endregion
                    case "MDW":
                        #region CASE "MDW"
                        {
                            log.Debug("Preparing to loop through workflow steps for an input source of MDW");

                            IBatchNode importStepNode = batchContext.GetStepNode(task.BatchNode, "IMPORT");
                            //Get page level node
                            importStepNode = importStepNode.GetDescendantNodes(0)[0];
                            
                            BatchDetail.Department = importStepNode.NodeData.ValueSet.ReadString("SourceIndicator", "");
                            BatchDetail.PrepDate = importStepNode.NodeData.ValueSet.ReadString("CreateDate", "");
                            BatchDetail.PrepDate += " " + importStepNode.NodeData.ValueSet.ReadString("CreateTime", "");
                            BatchDetail.ReceivedDate = BatchDetail.PrepDate;
                            BatchDetail.ReceivedDateCRD = BatchDetail.PrepDate;
                            if (BatchDetail.Department.Length > 0)
                            {
                                BatchDetail.SiteID = BatchDetail.Department.Substring(0, 3);
                            }



                            log.Debug("Completed setting batch details for an input source of MDW");
                            break;
                        }
                        #endregion
                    case "DROPBOX":
                        #region CASE "DROPBOX"
                        {
                            log.Debug("Preparing to loop through workflow steps for an input source of DROPBOX");

                            IBatchNode importStepNode = batchContext.GetStepNode(task.BatchNode, "IMPORT");
                            //Get page level node
                            importStepNode = importStepNode.GetDescendantNodes(0)[0];

                            BatchDetail.Department = importStepNode.NodeData.ValueSet.ReadString("SourceIndicator", "");
                            BatchDetail.PrepDate = importStepNode.NodeData.ValueSet.ReadString("CreateDate", "");
                            BatchDetail.PrepDate += " " + importStepNode.NodeData.ValueSet.ReadString("CreateTime", "");
                            BatchDetail.ReceivedDate = BatchDetail.PrepDate;
                            BatchDetail.ReceivedDateCRD = BatchDetail.PrepDate;
                            if (BatchDetail.Department.Length > 0)
                            {
                                BatchDetail.SiteID = BatchDetail.Department.Substring(0, 3);
                            }


                            log.Debug("Completed setting batch details for an input source of DROPBOX");
                            break;
                        }
                        #endregion
                }

            }
            catch (Exception ex)
            {
                log.Error("Error within the SetBatchDetails method: " + ex.Message, ex);
                throw ex;
            }
        }

        
        private void setBatchValues(IClientTask task, IBatchContext batchContext)
        {
            try
            {
                log.Debug("Preparing to loop through all workflow steps to set batch values");

                //START:AR122352
                CustomParameters parmsCustom = new CustomParameters();
                IBatchNode currentNode = task.BatchNode;
                parmsCustom.DSN = currentNode.StepData.StepConfiguration.ReadString("_DSN", "");
                parmsCustom.UserName = currentNode.StepData.StepConfiguration.ReadString("_USERNAME", "");
                parmsCustom.Password = currentNode.StepData.StepConfiguration.ReadString("_PASSWORD", "");
                //END:AR122352

                //setup all 3 manual touchpoints with the department selection and store the batch details
                try
                {

                    IBatchNode autoClassifyP7 = batchContext.GetRoot("AutoClassify");
                    autoClassifyP7.NodeData.ValueSet.WriteString("IADepartment", BatchDetail.Department);
                }
                catch (Exception ex)
                {
                    ex.ToString();
                }

                try
                {
                    IBatchNode autoValidationP7 = batchContext.GetRoot("AutoValidation");
                    autoValidationP7.NodeData.ValueSet.WriteString("IADepartment", BatchDetail.Department);
                }
                catch (Exception ex)
                {
                    ex.ToString();
                }

                try
                {
                    IBatchNode manualClassifyP7 = batchContext.GetRoot("ManualClassify");
                    manualClassifyP7.NodeData.ValueSet.WriteString("IADepartment", BatchDetail.Department);       
                }
                catch (Exception ex)
                {
                    ex.ToString();
                }

                try
                {
                    IBatchNode manualValP7 = batchContext.GetRoot("ManualVal");
                    manualValP7.NodeData.ValueSet.WriteString("IATaskRouting", BatchDetail.Department);
                }
                catch (Exception ex)
                {
                    ex.ToString();
                }

                try
                {
                    IBatchNode manualIndexP7 = batchContext.GetStepNode(task.BatchNode,"ManualIndex");
                    foreach (IBatchNode manualIndexP3 in manualIndexP7.GetDescendantNodes(3))
                    {
                        manualIndexP3.NodeData.ValueSet.WriteString("IATaskRouting", BatchDetail.Department);
                    }
                }
                catch (Exception ex)
                {
                    ex.ToString();
                }

                try
                {
                    IBatchNode standardMDFP7 = batchContext.GetRoot("Standard_MDF");
                    IValueAccessor accessor = standardMDFP7.NodeData.ValueSet;
                    accessor.WriteString("BATCH_NO", BatchDetail.BatchNo);
                    accessor.WriteString("BATCH_DEPARTMENT", BatchDetail.Department);
                    accessor.WriteString("BATCH_PRIORITY", BatchDetail.BatchPriority);
                    accessor.WriteString("SITE_ID", BatchDetail.SiteID);
                    accessor.WriteString("WORK_CATEGORY", BatchDetail.WorkCategory);
                    accessor.WriteString("BOX_NO", BatchDetail.BoxNo);
                    accessor.WriteString("PREP_OPERATOR", BatchDetail.PrepOperator);
                    accessor.WriteString("PREP_DATE", BatchDetail.PrepDate);
                    accessor.WriteString("SCANNER_ID", BatchDetail.ScannerID);
                    accessor.WriteString("RECEIVED_DATE", BatchDetail.ReceivedDate);
                    accessor.WriteString("CRD_RECEIVED_DATE", BatchDetail.ReceivedDateCRD);
                    accessor.WriteString("BATCH_AGEING", BatchDetail.BatchAgeing);
                    accessor.WriteString("MAX_CLAIMS_PER_DOC", BatchDetail.MaxClaimsPerDoc);
                    accessor.WriteString("TRACK_USER", BatchDetail.TrackUser);
                    accessor.WriteString("TRACK_PERFORMANCE", BatchDetail.TrackPerformance);
                    accessor.WriteString("DEFAULTAUTOREJECTPATH", BatchDetail.DefaultAutoRejectPath);
                    accessor.WriteString("DEFAULTREJECTPATH", BatchDetail.DefaultRejectPath);
                    accessor.WriteString("DELETEDBATCHPATH", BatchDetail.DeletedBatchPath);
                    accessor.WriteString("DIRECT2WORKFLOW", BatchDetail.Direct2Workflow);
                    accessor.WriteString("DISP_CITRIX_PATH", BatchDetail.DispCitrixPath);
                    accessor.WriteString("DISP_SERVER_PATH", BatchDetail.DispServerPath);
                    accessor.WriteString("SKIP_AUTOROTATE", BatchDetail.SkipAutoRotate);
                    accessor.WriteString("CREATE_MDW_COVER", BatchDetail.CreateMDWCover);

                    //START:AR122352
                    accessor.WriteString("GENERATE_XLS", BatchDetail.GenerateXLS);
                    accessor.WriteString("DEFAULT_XLS_PATH", BatchDetail.DefaultXLSPath);
                    setDBCredentials(task, batchContext, parmsCustom);
                    //END:AR122352

                }
                catch (Exception ex)
                {
                    ex.ToString();
                }
                
                log.Debug("Completed setting batch values");
            }
            catch (Exception ex)
            {
                log.Error("Error within the SetBatchValues method: " + ex.Message, ex);
            }
        }
        


        
        private void getEnvelopeDetails(IClientTask task, IBatchContext batchContext)
        {
            try
            {
                log.Debug("Preparing to loop through each envelope within the batch to get Envelope level values");
                IBatchNode node = task.BatchNode;
                IBatchNodeCollection envelopes = node.GetDescendantNodes(3);

                envelopeDetail = new EnvelopeDetail[envelopes.Count];
                int i = 0;
                foreach (IBatchNode env in envelopes)
                {
                    envelopeDetail[i] = new EnvelopeDetail();

                    switch (BatchDetail.InputSource)
                    {
                        case "EMAIL":
                            {
                                //TODO: Test
                                IBatchNode emailEnvNode = batchContext.GetStepNode(env, "EMAILIMPORT");
                                //emailImportStepNode.NodeData.ValueSet.ReadString("StartDate", "");
                                BatchDetail.PrepDate = emailEnvNode.NodeData.ValueSet.ReadString("StartDate", "");
                                BatchDetail.PrepDate += " " + emailEnvNode.NodeData.ValueSet.ReadString("StartTime", "");
                                BatchDetail.ReceivedDateCRD = BatchDetail.PrepDate;

                                envelopeDetail[i].EmailSender = emailEnvNode.NodeData.ValueSet.ReadString("EmailSenderAddr", "");
                                envelopeDetail[i].EmailTo = emailEnvNode.NodeData.ValueSet.ReadString("EmailAccount", "");
                                envelopeDetail[i].EmailDate = emailEnvNode.NodeData.ValueSet.ReadString("EmailDate", "");
                                envelopeDetail[i].EmailDate = Convert.ToDateTime(envelopeDetail[i].EmailDate).ToString("yyyy/MM/dd HH:mm:ss");
                                BatchDetail.ReceivedDate = envelopeDetail[i].EmailDate;
                                

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

        
        private void setEnvelopeValues(IClientTask task, IBatchContext batchContext)
        {
            try
            {
                log.Debug("Set Envelope values");
                IBatchNode currentNode = task.BatchNode;

                int i = 0;
                foreach (IBatchNode node in currentNode.GetDescendantNodes(3))
                {
                    IBatchNode standardMDFStepEnvNode = batchContext.GetStepNode(node, "STANDARD_MDF");
                    standardMDFStepEnvNode.NodeData.ValueSet.WriteString("E_DIRECT2WORKFLOW", BatchDetail.Direct2Workflow);
                    standardMDFStepEnvNode.NodeData.ValueSet.WriteString("E_EMAIL_SENDER", envelopeDetail[i].EmailSender);
                    if (BatchDetail.InputSource == "EMAIL")
                    {
                        //Set doc values
                        foreach (IBatchNode dnode in node.GetDescendantNodes(1))
                        {
                            IBatchNode standardMDFStepDocNode = batchContext.GetStepNode(dnode, "STANDARD_MDF");
                            standardMDFStepDocNode.NodeData.ValueSet.WriteString("D_EMAIL_SENDER", envelopeDetail[i].EmailSender);
                            standardMDFStepDocNode.NodeData.ValueSet.WriteString("D_EMAIL_TO", envelopeDetail[i].EmailTo);
                            standardMDFStepDocNode.NodeData.ValueSet.WriteString("D_EMAIL_DATE", envelopeDetail[i].EmailDate);
                        }
                    }
                    i++;
                }
                log.Debug("Completed setting envelope values");
            }
            catch (Exception ex)
            {
                log.Error("Error within the SetBatchValues method: " + ex.Message, ex);
            }
        }
         

        
        private void sendBatchNo2DB(IClientTask task, IBatchContext batchContext)
        {

            //we need to loop through each document in the batch 
            //and update the db with the batch it entered into
            foreach (IBatchNode node in task.BatchNode.Root.GetDescendantNodes(1))
            {

                IBatchNode standardMDFStepNode = batchContext.GetStepNode(node, "STANDARD_MDF");
                //based on what the input source is we have to do different things
                switch (BatchDetail.InputSource)
                {
                    case "FAX":
                        {
                            //go to the db and update using fax key and fax id
                            
                            DocumentDetail.FaxID = standardMDFStepNode.NodeData.ValueSet.ReadString("D_FAX_ID", "");
                            DocumentDetail.FaxKey = standardMDFStepNode.NodeData.ValueSet.ReadString("D_FAX_KEY", "");

                            //Test Start
                            string faxIDAtEnv = standardMDFStepNode.GetAncestor(3).NodeData.ValueSet.ReadString("D_FAX_ID");

                            //Test End


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
                            DocumentDetail.dGUID = standardMDFStepNode.NodeData.ValueSet.ReadString("D_GUID", "");
                            log.Debug("Preparing to update the db for dropbox GUID " + DocumentDetail.dGUID +
                                " indicating it was placed in batch " + BatchDetail.BatchNo);
                            //now call the update
                            _dbAccess.updateImportXref();
                            break;
                        }
                }
            }

        }

        //START:AR122352
        
        private void setDBCredentials(IClientTask task, IBatchContext batchContext, CustomParameters parmsCustom)
        {
            try
            {

                //grab the db credentials and store them in the process for later use 
                IBatchNode standardMDFStepNode = batchContext.GetStepNode(task.BatchNode, "STANDARD_MDF");
                standardMDFStepNode.NodeData.ValueSet.WriteString("DB_USER", parmsCustom.UserName);
                standardMDFStepNode.NodeData.ValueSet.WriteString("DB_PASS", parmsCustom.Password);
                standardMDFStepNode.NodeData.ValueSet.WriteString("DSN", parmsCustom.DSN);


            }
            catch (Exception ex)
            {
                log.Error("Error within the setDBCredentials method: " + ex.Message, ex);
            }
        }
        //END:AR122352
        


    }
}
