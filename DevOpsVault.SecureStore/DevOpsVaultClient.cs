using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DevOpsVault.SDK.Core.Api;
using DevOpsVault.SDK.Core.Client;
using DevOpsVault.SDK.Core.Model;
using UiPath.Orchestrator.Extensibility.SecureStores;

namespace DevOpsVault.SecureStore
{
    public interface IDevOpsVaultClient
    {
        Task<string> CreateSecretAsync(string path, string secretValue,
            CancellationToken cancellationToken = default);

        Task<string> UpdateSecretAsync(string newPath, string oldPath, string secretValue,
            CancellationToken cancellationToken = default);

        Task<string> UpdateSecretAsync(string newPath, string oldPath, Credential credential,
            CancellationToken cancellationToken = default);

        Task<string> CreateSecretAsync(string path, Credential credential,
            CancellationToken cancellationToken = default);

        Task<SecretResponse> GetSecretAsync(string path, CancellationToken cancellationToken = default);
        Task DeleteSecretAsync(string path, CancellationToken cancellationToken = default);
    }

    public class DevOpsVaultClient : IDevOpsVaultClient
    {
        private readonly ISecretsApi _secretsClient;
        private readonly DevOpsVaultContext _context;


        public DevOpsVaultClient(ISecretsApi secretsClient, DevOpsVaultContext context)
        {
            _secretsClient = secretsClient;
            _context = context;
        }

        public async Task<string> CreateSecretAsync(string path, string secretValue,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var slug = Normalize($"{_context.BasePathPrefix}:{path}");
                var response = await _secretsClient.CreateSecretAsync(slug, new SecretUpsert(
                    data: new Dictionary<string, object>()
                    {
                        {"password", secretValue},
                    }));
                return response?.Path;
            }
            catch (ApiException ex)
            {
                throw ConvertException(ex);
            }
        }

        public async Task<string> UpdateSecretAsync(string newPath, string oldPath, string secretValue,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var secretData = new Dictionary<string, object>()
                {
                    {"password", secretValue},
                };
                return await UpdateSecretInternal(newPath, oldPath, secretData);
            }
            catch (ApiException ex)
            {
                throw ConvertException(ex);
            }
        }

        public async Task<string> UpdateSecretAsync(string newPath, string oldPath, Credential credential,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var secretData = new Dictionary<string, object>()
                {
                    {"username", credential.Username},
                    {"password", credential.Password},
                };
                return await UpdateSecretInternal(newPath, oldPath, secretData);
            }
            catch (ApiException ex)
            {
                throw ConvertException(ex);
            }
        }


        public async Task<string> CreateSecretAsync(string path, Credential credential,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var slug = Normalize($"{_context.BasePathPrefix}:{path}");
                var upsert = new Dictionary<string, object>()
                {
                    {"username", credential.Username},
                    {"password", credential.Password},
                };
                var response = await _secretsClient.CreateSecretAsync(slug, new SecretUpsert(data: upsert));
                return response?.Path;
            }
            catch (ApiException ex)
            {
                throw ConvertException(ex);
            }
        }

        public async Task<SecretResponse> GetSecretAsync(string path, CancellationToken cancellationToken = default)
        {
            try
            {
                var slug = Normalize(path);
                return await _secretsClient.GetSecretAsync(slug);
            }
            catch (ApiException ex)
            {
                throw ConvertException(ex);
            }
        }

        public async Task DeleteSecretAsync(string path, CancellationToken cancellationToken = default)
        {
            try
            {
                var slug = Normalize(path);
                await _secretsClient.DeleteSecretAsync(slug);
            }
            catch (ApiException ex)
            {
                throw ConvertException(ex);
            }
        }

        private async Task<string> UpdateSecretInternal(string newPath, string oldPath, Dictionary<string, object> data)
        {
            var slug = Normalize($"{_context.BasePathPrefix}:{newPath}");

            if (string.Equals(slug, oldPath, StringComparison.InvariantCultureIgnoreCase))
            {
                var updateResponse = await _secretsClient.UpdateSecretAsync(slug,
                    new SecretUpsert(data: data));
                return updateResponse?.Path;
            }

            var response = await _secretsClient.CreateSecretAsync(slug, new SecretUpsert(
                data: data)
            );

            try
            {
                await _secretsClient.DeleteSecretAsync(Normalize(oldPath));
            }
            catch (ApiException ex)
            {
                //ignore if we fail to delete the secret
            }

            return response?.Path;
        }

        private static string Normalize(string inputPath)
        {
            inputPath = inputPath.Replace("\\", "-").Trim().ToLower();
            inputPath = inputPath.Replace("/", ":");
            return inputPath;
        }

        private SecureStoreException ConvertException(ApiException apiEx)
        {
            switch (apiEx.ErrorCode)
            {
                case (int) System.Net.HttpStatusCode.Forbidden:
                    return new SecureStoreException(SecureStoreException.Type.UnauthorizedOperation,
                        DevOpsVaultResource.GetResource("AccessDenied"),
                        apiEx);
                case (int) System.Net.HttpStatusCode.NotFound:
                    return new SecureStoreException(SecureStoreException.Type.SecretNotFound,
                        DevOpsVaultResource.GetResource("SecretNotFound"),
                        apiEx);
                default:
                    return new SecureStoreException(DevOpsVaultResource.GetResource("GenericError"), apiEx);
            }
        }
    }
}