using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UiPath.Orchestrator.Extensibility.Configuration;
using UiPath.Orchestrator.Extensibility.SecureStores;

namespace DevOpsVault.SecureStore
{
    public class DevOpsVaultSecureStore : ISecureStore
    {
        private readonly IDevOpsVaultClientFactory _clientFactory;
        private IDevOpsVaultClient _dsvClient;

        public DevOpsVaultSecureStore() : this(new DevOpsVaultClientFactory())
        {
        }

        public DevOpsVaultSecureStore(IDevOpsVaultClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public void Initialize(Dictionary<string, string> hostSettings)
        {
        }

        public SecureStoreInfo GetStoreInfo()
        {
            return new SecureStoreInfo {Identifier = "DevOpsSecretsVault", IsReadOnly = false};
        }

        public async Task ValidateContextAsync(string context)
        {
            var id = Guid.NewGuid();
            var ctx = DeserializeContext(context);
            _dsvClient = await _clientFactory.GetClient(ctx);
            var response = await _dsvClient.CreateSecretAsync($"validate:{id}", id.ToString());
            await _dsvClient.DeleteSecretAsync(response);
        }

        public IEnumerable<ConfigurationEntry> GetConfiguration()
        {
            return new List<ConfigurationEntry>
            {
                new ConfigurationValue(ConfigurationValueType.String)
                {
                    Key = "DevOpsVaultUrl",
                    DisplayName = DevOpsVaultResource.GetResource("DevOpsVaultUrl"),
                    IsMandatory = true,
                },
                new ConfigurationValue(ConfigurationValueType.String)
                {
                    Key = "ClientId",
                    DisplayName = DevOpsVaultResource.GetResource("ClientId"),
                    IsMandatory = true,
                },
                new ConfigurationValue(ConfigurationValueType.String)
                {
                    Key = "ClientSecret",
                    DisplayName = DevOpsVaultResource.GetResource("ClientSecret"),
                    IsMandatory = true,
                },
                new ConfigurationValue(ConfigurationValueType.String)
                {
                    Key = "BasePathPrefix",
                    DisplayName = DevOpsVaultResource.GetResource("BasePathPrefix"),
                    DefaultValue = "uipath",
                },
            };
        }

        public async Task<string> CreateValueAsync(string context, string key, string value)
        {
            var ctx = DeserializeContext(context);
            value = value ?? throw new ArgumentNullException(nameof(value));
            _dsvClient = await _clientFactory.GetClient(ctx);
            var response = await _dsvClient.CreateSecretAsync(key, value);
            return response;
        }

        public async Task<string> GetValueAsync(string context, string key)
        {
            var ctx = DeserializeContext(context);
            key = key ?? throw new SecureStoreException();
            _dsvClient = await _clientFactory.GetClient(ctx);
            var response = await _dsvClient.GetSecretAsync(key);
            return response?.Data["password"].ToString();
        }

        public async Task<string> CreateCredentialsAsync(string context, string key, Credential value)
        {
            var ctx = DeserializeContext(context);
            value = value ?? throw new ArgumentNullException(nameof(value));
            _dsvClient = await _clientFactory.GetClient(ctx);
            var response = await _dsvClient.CreateSecretAsync(key, value);
            return response;
        }

        public async Task<Credential> GetCredentialsAsync(string context, string key)
        {
            var ctx = DeserializeContext(context);
            key = key ?? throw new SecureStoreException();
            _dsvClient = await _clientFactory.GetClient(ctx);
            var response = await _dsvClient.GetSecretAsync(key);
            return new Credential()
            {
                Username = response.Data["username"].ToString(),
                Password = response.Data["password"].ToString(),
            };
        }

        public async Task<string> UpdateValueAsync(string context, string key, string oldAugumentedKey, string value)
        {
            var ctx = DeserializeContext(context);
            key = key ?? throw new SecureStoreException();
            _dsvClient = await _clientFactory.GetClient(ctx);
            var response = await _dsvClient.UpdateSecretAsync(key, oldAugumentedKey, value);
            return response;
        }

        public async Task<string> UpdateCredentialsAsync(string context, string key, string oldAugumentedKey,
            Credential value)
        {
            var ctx = DeserializeContext(context);
            key = key ?? throw new SecureStoreException();
            _dsvClient = await _clientFactory.GetClient(ctx);
            var response = await _dsvClient.UpdateSecretAsync(key, oldAugumentedKey, value);
            return response;
        }

        public async Task RemoveValueAsync(string context, string key)
        {
            var ctx = DeserializeContext(context);
            key = key ?? throw new SecureStoreException();
            _dsvClient = await _clientFactory.GetClient(ctx);
            await _dsvClient.DeleteSecretAsync(key);
        }

        private DevOpsVaultContext DeserializeContext(string context)
        {
            var dsvConfig = JsonConvert.DeserializeObject<DevOpsVaultContext>(context);
            return dsvConfig;
        }
    }
}