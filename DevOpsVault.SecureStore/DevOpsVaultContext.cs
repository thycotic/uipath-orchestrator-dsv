using System;

namespace DevOpsVault.SecureStore
{
    public class DevOpsVaultContext
    {
        public Uri DevOpsVaultUrl { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string BasePathPrefix { get; set; }
    }
}