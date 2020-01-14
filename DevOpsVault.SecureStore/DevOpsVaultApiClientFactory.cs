using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevOpsVault.SDK.Core.Api;

namespace DevOpsVault.SecureStore
{
    public interface IDevOpsVaultApiClientFactory
    {
        ITokensApi GetTokensApi();
        ISecretsApi GetSecretsApi();
    }

    public class DevOpsVaultApiClientFactory : IDevOpsVaultApiClientFactory
    {
        public ITokensApi GetTokensApi()
        {
            return new TokensApi();
        }

        public ISecretsApi GetSecretsApi()
        {
            return new SecretsApi();
        }
    }
}
