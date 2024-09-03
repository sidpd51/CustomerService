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

        public EntityCollection RetrieveEntities(string entityName, ColumnSet columns, string filter = null, string sortColumn = null, bool sortDescending = false, int pageNumber = 1,int pageSize = 50)
        {

            var query = new QueryExpression(entityName)
            {
                ColumnSet = columns,
                PageInfo = new PagingInfo
                {
                    PageNumber = pageNumber,
                    Count = pageSize
                }
            };

            if (!string.IsNullOrEmpty(sortColumn))
            {
                query.Orders.Add(new OrderExpression(sortColumn, sortDescending ? OrderType.Descending : OrderType.Ascending));
            }

            // Apply filtering if specified
            if (!string.IsNullOrEmpty(filter))
            {
                var filterExpression = new FilterExpression(LogicalOperator.Or);

                // Use filter to match string fields
                filterExpression.AddCondition(new ConditionExpression("title", ConditionOperator.Like, $"%{filter}%"));
                filterExpression.AddCondition(new ConditionExpression("description", ConditionOperator.Like, $"%{filter}%"));
                filterExpression.AddCondition(new ConditionExpression("ticketnumber", ConditionOperator.Like, $"%{filter}%"));

                // Add conditions for OptionSetValue fields
                if (int.TryParse(filter, out int filterValue))
                {
                    filterExpression.AddCondition(new ConditionExpression("prioritycode", ConditionOperator.Equal, filterValue));
                    filterExpression.AddCondition(new ConditionExpression("statuscode", ConditionOperator.Equal, filterValue));
                }
                else
                {
                    // Add conditions for OptionSetValue fields if filter is not numeric
                    filterExpression.AddCondition(new ConditionExpression("prioritycode", ConditionOperator.Like, $"%{filter}%"));
                    filterExpression.AddCondition(new ConditionExpression("statuscode", ConditionOperator.Like, $"%{filter}%"));
                }

                query.Criteria = filterExpression;
            }

            // Execute the query and retrieve results
            var result = _serviceClient.RetrieveMultiple(query);
            return result;
        }


        public Guid GetCurrentUserId()
        {
            var request = new WhoAmIRequest();
            var response = (WhoAmIResponse)_serviceClient.Execute(request);
            return response.UserId;
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
                

            }catch(Exception ex)
            {
                throw new ApplicationException($"An error occurred while resolving the Case entity with ID {caseId}.", ex);
            }
        }

    }
}
