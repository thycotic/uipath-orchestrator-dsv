using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using DevOpsVault.SDK.Core.Api;
using DevOpsVault.SDK.Core.Client;
using UiPath.Orchestrator.Extensibility.SecureStores;

namespace DevOpsVault.SecureStore
{
    public interface IDevOpsVaultClientFactory
    {
        Task<IDevOpsVaultClient> GetClient(DevOpsVaultContext context);
    }

    public class DevOpsVaultClientFactory : IDevOpsVaultClientFactory
    {
        private static readonly IDictionary<string, AccessTokenInfo> TokenCache;
        private readonly IDevOpsVaultApiClientFactory _apiClientFactory;

        static DevOpsVaultClientFactory()
        {
            TokenCache = new ConcurrentDictionary<string, AccessTokenInfo>();
        }

        public DevOpsVaultClientFactory(IDevOpsVaultApiClientFactory apiClientFactory)
        {
            _apiClientFactory = apiClientFactory;
        }

        public DevOpsVaultClientFactory() : this(new DevOpsVaultApiClientFactory())
        {
        }

        public async Task<IDevOpsVaultClient> GetClient(DevOpsVaultContext context)
        {
            bool needsRefresh = true;

            var config = new Configuration
            {
                BasePath = BuildTenantUri(context.DevOpsVaultUrl.AbsoluteUri), Timeout = 2000
            };
            GlobalConfiguration.Instance = Configuration.MergeConfigurations(GlobalConfiguration.Instance, config);

            if (TokenCache.ContainsKey(context.ClientId))
            {
                var timeToRefresh = TokenCache[context.ClientId].ExpirationTime.AddMinutes(-10);
                if (timeToRefresh > DateTime.UtcNow)
                {
                    needsRefresh = false;
                }
            }

            if (needsRefresh)
            {
                var tokensApi = _apiClientFactory.GetTokensApi();

                var response = await tokensApi.TokenAsync("client_credentials", clientId: context.ClientId,
                    clientSecret: context.ClientSecret);

                if (response == null || string.IsNullOrEmpty(response.AccessToken))
                {
                    throw new SecureStoreException(SecureStoreException.Type.InvalidConfiguration,
                        DevOpsVaultResource.GetResource("UnableToAuthenticate"));
                }

                TokenCache[context.ClientId] = new AccessTokenInfo
                {
                    AccessToken = response.AccessToken,
                    ExpirationTime = DateTime.UtcNow.AddSeconds(response.ExpiresIn)
                };
            }

            config.AccessToken = TokenCache[context.ClientId].AccessToken;

            GlobalConfiguration.Instance = Configuration.MergeConfigurations(GlobalConfiguration.Instance, config);
            var secretsApi = _apiClientFactory.GetSecretsApi();
            return new DevOpsVaultClient(secretsApi, context);
        }

        private string BuildTenantUri(string absoluteUri)
        {
            if (absoluteUri.EndsWith("/"))
            {
                absoluteUri = absoluteUri.TrimEnd('/');
            }

            if (!absoluteUri.EndsWith("v1"))
            {
                absoluteUri = absoluteUri + "/v1";
            }

            return absoluteUri;
        }
    }

    class AccessTokenInfo
    {
        public string AccessToken { get; set; }
        public DateTime ExpirationTime { get; set; }
    }
}