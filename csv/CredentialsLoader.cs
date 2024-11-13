using Microsoft.Extensions.Configuration;

namespace AzureBlobCleaner
{
    public class CredentialsLoader
    {
        private readonly IConfiguration _configuration;

        public CredentialsLoader()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddUserSecrets<CredentialsLoader>();

            _configuration = builder.Build();
        }

        public string GetAzureConnectionString()
        {
            return _configuration["Azure:Credentials"];
        }
    }
}

