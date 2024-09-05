using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;


namespace CustomerService
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

    }
}
