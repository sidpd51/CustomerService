using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;


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
    }
}
