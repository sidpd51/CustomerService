using Microsoft.Identity.Client;

namespace CustomerService
{
    public class AuthenticationProvider
    {
        private readonly IConfiguration _configuration;
        public AuthenticationProvider(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GetAccessToken()
        {
            var clientId = _configuration["AzureAd:ClientId"];
            var clientSecret = _configuration["AzureAd:ClientSecret"];
            var authority = $"{_configuration["AzureAd:Instance"]}{_configuration["AzureAd:TenantId"]}";

            var app = ConfidentialClientApplicationBuilder.Create(clientId).WithClientSecret(clientSecret).WithAuthority(new Uri(authority)).Build();

            var scopes = new string[] { $"{_configuration["Dataverse:DataverseUrl"]}/.default" };

            var result = app.AcquireTokenForClient(scopes).ExecuteAsync().Result;

            return result.AccessToken;
        }
    }
}