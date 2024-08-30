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
