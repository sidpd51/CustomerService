using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;


namespace CustomerService.Service
{
    public class DataverseService
    {
        private readonly ServiceClient _serviceClient;

        public DataverseService(IConfiguration configuration)
        {
            var connectionString = configuration["Dataverse:ConnectionString"];
            _serviceClient = new ServiceClient(connectionString);
        }

        public EntityCollection RetrieveEtities(string entityName, ColumnSet columns)
        {
            var query = new QueryExpression(entityName)
            {
                ColumnSet = columns
            };
            return _serviceClient.RetrieveMultiple(query);
        }

 
        public EntityCollection QualiableData(string entityName,int skip, int pageSize, ColumnSet columns, string filter, string sortColumn="createdon", string sortDescending = "desc")
        {
            var orderBy = (sortDescending == "asc" ? OrderType.Ascending : OrderType.Descending);
            var query = new QueryExpression(entityName) {
                ColumnSet = columns,
                Distinct = false,
                Criteria =
                {
                    Filters =
                    {
                        new FilterExpression
                        {
                            FilterOperator = LogicalOperator.Or,
                            Conditions =
                            {
                                new ConditionExpression(
                                    attributeName: "title",
                                    conditionOperator: ConditionOperator.Like,
                                    values: new[] { "%" + filter + "%" }
                                    ),
                                new ConditionExpression(
                                    attributeName: "ticketnumber",
                                    conditionOperator: ConditionOperator.Like,
                                    values: new[] { "%" + filter + "%" }
                                    )
                            }
                        }
                    }
                },
                PageInfo =
                {
                    Count=pageSize,
                    PageNumber=(skip/pageSize)+1,
                    ReturnTotalRecordCount =true
                }
            };
            if (sortColumn != null)
            {
                string orderCol = sortColumn.ToLower();
                query.AddOrder(attributeName: orderCol, orderType: orderBy);

            }
            

            return _serviceClient.RetrieveMultiple(query);
        }

        public Guid CreateEntity (Entity entity)
        {
            if(_serviceClient == null)
            {
                throw new InvalidOperationException("Dataverse service client is not initialized.");
            }

            return _serviceClient.Create(entity);
        }

        public Entity RetrieveEntity(string entityName, Guid id, ColumnSet columns) { 
            if(_serviceClient == null)
            {
                throw new InvalidOperationException("Dataverse service client is not initiazlied");
            }
            try
            {
                Entity entity = _serviceClient.Retrieve(entityName, id, columns);
                return entity;
                
            }catch(Exception ex)
            {
                throw new ApplicationException($"An error occurred while retrieving the {entityName} entity with Id: {id}", ex);
            }
        }

        public void UpdateEntity(Entity entity)
        {
            if(_serviceClient == null)
            {
                throw new InvalidOperationException("Dataverse service client is not initialized.");

            }
            _serviceClient.Update(entity);
        }

        public void DeleteEntity(string entityName, Guid id)
        {
            if (_serviceClient == null)
            {
                throw new InvalidOperationException("Dataverse service client is not initiazlied");
            }

            try
            {
                _serviceClient.Delete(entityName, id);
            }
            catch(Exception ex) {
                throw new ApplicationException($"An error occurred while deleting the {entityName} entity with ID {id}.", ex);
            }
        }

        public void SetCaseStatus(Guid caseId, int status)
        {
            if (_serviceClient == null)
            {
                throw new InvalidOperationException("Dataverse service client is not initiazlied");
            }
            try
            {
                if(status == 5)
                {
                    EntityReference caseReference = new EntityReference("incident", caseId);

                    Entity resolution = new("incidentresolution")
                    {
                        Attributes =
                    {
                        {"subject","Case Closed" },
                        {"incidentid", caseReference }
                    }
                    };

                    CloseIncidentRequest request = new()
                    {
                        IncidentResolution = resolution,
                        Status = new OptionSetValue(status)
                    };

                    _serviceClient.Execute(request);
                    return;
                }
                else if (status == 6)
                {
                   
                    Entity incident = new Entity("incident")
                    {
                        Id = caseId,
                    };

                    incident["statecode"] = new OptionSetValue(2);
                    incident["statuscode"] = new OptionSetValue(status);

                    UpdateRequest updateRequest = new()
                    {
                        Target = incident
                    };

                    _serviceClient.Execute(updateRequest);
                    return;
                    
                }else
                {
                    Entity incident = new Entity("incident")
                    {
                        Id = caseId,
                    };

                    incident["statecode"] = new OptionSetValue(0);
                    incident["statuscode"] = new OptionSetValue(status);

                    UpdateRequest updateRequest = new()
                    {
                        Target = incident
                    };

                    _serviceClient.Execute(updateRequest);
                    return;
                }

            }
            catch(Exception ex)
            {
                throw new ApplicationException($"An error occurred while resolving the Case entity with ID {caseId}.", ex);
            }
        }

        public Guid GetCurrentUserGuid(string id)
        {
            try
            {
                QueryExpression query = new()
                {
                    Distinct = false,
                    EntityName = "systemuser",
                    ColumnSet = new ColumnSet("firstname", "systemuserid"),
                    Criteria =
                {
                    Filters =
                    {
                        new FilterExpression
                        {
                           FilterOperator = LogicalOperator.Or,
                           Conditions =
                            {
                                new ConditionExpression(
                                    attributeName: "azureactivedirectoryobjectid",
                                    conditionOperator: ConditionOperator.Equal,
                                    values: id
                                    )
                            }
                        }
                    }
                }

                };
                return _serviceClient.RetrieveMultiple(query).Entities.FirstOrDefault().GetAttributeValue<Guid>("systemuserid");
            }
            catch (Exception)
            {

                throw;
            }
        }

        public EntityCollection RetrieveContacts(ColumnSet columns)
        {
            QueryExpression query = new QueryExpression("contact")
            {
                ColumnSet = columns
            };
            return _serviceClient.RetrieveMultiple(query);
        }

        public InitializeFileBlocksUploadResponse ExecuteFileBlockUpload(InitializeFileBlocksUploadRequest fileblock)
        {
            try
            {
                // Execute the request and cast the response to the appropriate type
                return (InitializeFileBlocksUploadResponse)_serviceClient.Execute(fileblock);
            }
            catch (Exception)
            {
                // Handle or log the exception as needed
                throw;
            }
        }

        
        public UploadBlockResponse ExecuteUploadBlock(UploadBlockRequest blockRequest)
        {
            try
            {
                return (UploadBlockResponse)_serviceClient.Execute(blockRequest);
            }
            catch (Exception ex)
            {
              
                throw;
            }
        }

        public void CommitOperation(List<string>? blockIds,
            string fileContinuationToken,
            string fileName,
            string mimeType)
        {
            try
            {
                 var commitRequest = new CommitFileBlocksUploadRequest()
                {
                    BlockList = blockIds.ToArray(),
                    FileContinuationToken = fileContinuationToken,
                    FileName = fileName,
                    MimeType = mimeType
                };

                // Execute the commit request
                _serviceClient.Execute(commitRequest);
            }
            catch (Exception ex)
            {
                // Handle or log the exception as needed
                // For example, you could log the exception:
                // Log.Error("An error occurred while committing the operation.", ex);

                // Re-throw the exception if it cannot be handled here
                throw;
            }
        }

        /// <summary>
        /// Downloads a file or image
        /// </summary>
        /// <param name="service">The service</param>
        /// <param name="entityReference">A reference to the record with the file or image column</param>
        /// <param name="attributeName">The name of the file or image column</param>
        /// <returns></returns>
        public  byte[] DownloadFile(
                    EntityReference entityReference,
                    string attributeName)
        {
            InitializeFileBlocksDownloadRequest initializeFileBlocksDownloadRequest = new()
            {
                Target = entityReference,
                FileAttributeName = attributeName
            };

            var initializeFileBlocksDownloadResponse =
                  (InitializeFileBlocksDownloadResponse)_serviceClient.Execute(initializeFileBlocksDownloadRequest);

            string fileContinuationToken = initializeFileBlocksDownloadResponse.FileContinuationToken;
            long fileSizeInBytes = initializeFileBlocksDownloadResponse.FileSizeInBytes;

            List<byte> fileBytes = new((int)fileSizeInBytes);

            long offset = 0;
            // If chunking is not supported, chunk size will be full size of the file.
            long blockSizeDownload = !initializeFileBlocksDownloadResponse.IsChunkingSupported ? fileSizeInBytes : 4 * 1024 * 1024;

            // File size may be smaller than defined block size
            if (fileSizeInBytes < blockSizeDownload)
            {
                blockSizeDownload = fileSizeInBytes;
            }

            while (fileSizeInBytes > 0)
            {
                // Prepare the request
                DownloadBlockRequest downLoadBlockRequest = new()
                {
                    BlockLength = blockSizeDownload,
                    FileContinuationToken = fileContinuationToken,
                    Offset = offset
                };

                // Send the request
                var downloadBlockResponse =
                         (DownloadBlockResponse)_serviceClient.Execute(downLoadBlockRequest);

                // Add the block returned to the list
                fileBytes.AddRange(downloadBlockResponse.Data);

                // Subtract the amount downloaded,
                // which may make fileSizeInBytes < 0 and indicate
                // no further blocks to download
                fileSizeInBytes -= (int)blockSizeDownload;
                // Increment the offset to start at the beginning of the next block.
                offset += blockSizeDownload;
            }

            return fileBytes.ToArray();
        }

    }
}
