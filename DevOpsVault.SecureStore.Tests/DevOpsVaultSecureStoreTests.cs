using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevOpsVault.SDK.Core.Api;
using DevOpsVault.SDK.Core.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using UiPath.Orchestrator.Extensibility.SecureStores;

namespace DevOpsVault.SecureStore.Tests
{
    [TestClass]
    public class DevOpsVaultSecureStoreTests
    {
        private ITokensApi _tokensApi;
        private ISecretsApi _secretsApi;
        private DevOpsVaultClientFactory _clientFactory;
        private DevOpsVaultSecureStore _secureStore;
        private DevOpsVaultContext _devOpsContext;
        private string _ctxString;
        private IDevOpsVaultApiClientFactory _apiClientFactory;

        [TestInitialize]
        public void TestInitialize()
        {
            _tokensApi = Substitute.For<ITokensApi>();
            _secretsApi = Substitute.For<ISecretsApi>();
            _apiClientFactory = Substitute.For<IDevOpsVaultApiClientFactory>();
            _apiClientFactory.GetTokensApi().Returns(_tokensApi);
            _apiClientFactory.GetSecretsApi().Returns(_secretsApi);
            _clientFactory = new DevOpsVaultClientFactory(_apiClientFactory);
            _secureStore = new DevOpsVaultSecureStore(_clientFactory);
            _devOpsContext = new DevOpsVaultContext();
            _devOpsContext.BasePathPrefix = "uipath";
            _devOpsContext.DevOpsVaultUrl = new Uri("https://test.secretsvault.fake");
            _devOpsContext.ClientId = "test-client";
            _devOpsContext.ClientSecret = "test-secret";
            _ctxString = JsonConvert.SerializeObject(_devOpsContext);
        }

        [TestMethod]
        public async Task TestCreateValue()
        {
            var secretPath = Guid.NewGuid().ToString();
            var value =  Guid.NewGuid().ToString();

            var tokenReturn =  Task.FromResult(new AccessTokenResponse("access-token", 3600, tokenType: "bearer"));
            _tokensApi.TokenAsync("client_credentials", clientId: _devOpsContext.ClientId,
                    clientSecret: _devOpsContext.ClientSecret)
                .Returns(tokenReturn);

            var secretReturn =
                Task.FromResult(new SecretResponse(path: $"{_devOpsContext.BasePathPrefix}:{secretPath}"));
            _secretsApi.CreateSecretAsync($"{_devOpsContext.BasePathPrefix}:{secretPath}",
                    Arg.Is<SecretUpsert>(x => x.Data["password"].Equals(value)))
                .Returns(secretReturn);

            

            var result = await _secureStore.CreateValueAsync(_ctxString, secretPath, value);

            Assert.AreEqual($"{_devOpsContext.BasePathPrefix}:{secretPath}", result);
        }

        [TestMethod]
        public async Task TestCreateCredential()
        {
            var secretPath = Guid.NewGuid().ToString();
            var value = new Credential {Password = Guid.NewGuid().ToString(), Username = Guid.NewGuid().ToString()};

            var tokenReturn =  Task.FromResult(new AccessTokenResponse("access-token", 3600, tokenType: "bearer"));
            _tokensApi.TokenAsync("client_credentials", clientId: _devOpsContext.ClientId,
                    clientSecret: _devOpsContext.ClientSecret)
                .Returns(tokenReturn);

            var secretReturn =
                Task.FromResult(new SecretResponse(path: $"{_devOpsContext.BasePathPrefix}:{secretPath}"));
            _secretsApi.CreateSecretAsync($"{_devOpsContext.BasePathPrefix}:{secretPath}",
                    Arg.Is<SecretUpsert>(x => x.Data["password"].Equals(value.Password) && x.Data["username"].Equals(value.Username)))
                .Returns(secretReturn);

            var result = await _secureStore.CreateCredentialsAsync(_ctxString, secretPath, value);

            Assert.AreEqual($"{_devOpsContext.BasePathPrefix}:{secretPath}", result);
        }

        [TestMethod]
        public async Task TestGetValue()
        {
            var secretPath = $"{_devOpsContext.BasePathPrefix}:{Guid.NewGuid().ToString()}";
            var value = Guid.NewGuid().ToString();

            var tokenReturn =  Task.FromResult(new AccessTokenResponse("access-token", 3600, tokenType: "bearer"));
            _tokensApi.TokenAsync("client_credentials", clientId: _devOpsContext.ClientId,
                    clientSecret: _devOpsContext.ClientSecret)
                .Returns(tokenReturn);

            var secretReturn = Task.FromResult(new SecretResponse(path: secretPath,
                data: new Dictionary<string, object>() {{"password", value}}));
            _secretsApi.GetSecretAsync(secretPath)
                .Returns(secretReturn);

            var result = await _secureStore.GetValueAsync(_ctxString, secretPath);

            Assert.AreEqual(value, result);
        }

        [TestMethod]
        public async Task TestGetCredential()
        {
            var secretPath = $"{_devOpsContext.BasePathPrefix}:{Guid.NewGuid().ToString()}";
            var value = new Credential() {Username = Guid.NewGuid().ToString(), Password = Guid.NewGuid().ToString()};

            var tokenReturn =  Task.FromResult(new AccessTokenResponse("access-token", 3600, tokenType: "bearer"));
            _tokensApi.TokenAsync("client_credentials", clientId: _devOpsContext.ClientId,
                    clientSecret: _devOpsContext.ClientSecret)
                .Returns(tokenReturn);

            var secretReturn = Task.FromResult(new SecretResponse(path: secretPath,
                data: new Dictionary<string, object>() {{"password", value.Password}, {"username", value.Username}}));
            _secretsApi.GetSecretAsync(secretPath)
                .Returns(secretReturn);

            var result = await _secureStore.GetCredentialsAsync(_ctxString, secretPath);

            Assert.AreEqual(value.Username, result.Username);
            Assert.AreEqual(value.Password, result.Password);
        }

        [TestMethod]
        public async Task TestRemove()
        {
            var secretPath = $"{_devOpsContext.BasePathPrefix}:{Guid.NewGuid().ToString()}";

            var tokenReturn =  Task.FromResult(new AccessTokenResponse("access-token", 3600, tokenType: "bearer"));
            _tokensApi.TokenAsync("client_credentials", clientId: _devOpsContext.ClientId,
                    clientSecret: _devOpsContext.ClientSecret)
                .Returns(tokenReturn);

            await _secureStore.RemoveValueAsync(_ctxString, secretPath);

            await _secretsApi.Received().DeleteSecretAsync(secretPath);
        }

        [TestMethod]
        public async Task TestUpdateValue()
        {
            var secretPath = Guid.NewGuid().ToString();
            var oldSecretPath = $"{_devOpsContext.BasePathPrefix}:{secretPath}";
            var value = Guid.NewGuid().ToString();

            var tokenReturn = new AccessTokenResponse("access-token", 3600, tokenType: "bearer");
            _tokensApi.Token("client_credentials", clientId: _devOpsContext.ClientId,
                    clientSecret: _devOpsContext.ClientSecret)
                .Returns(tokenReturn);

            var secretReturn = Task.FromResult(new SecretResponse(path: oldSecretPath));
            _secretsApi.UpdateSecretAsync(oldSecretPath,
                    Arg.Is<SecretUpsert>(x => x.Data["password"].Equals(value)))
                .Returns(secretReturn);

            var result = await _secureStore.UpdateValueAsync(_ctxString, secretPath, oldSecretPath, value);
            
            Assert.AreEqual(oldSecretPath, result);
        }

        [TestMethod]
        public async Task TestUpdateCredential()
        {
            var secretPath = Guid.NewGuid().ToString();
            var oldSecretPath = $"{_devOpsContext.BasePathPrefix}:{secretPath}";
            var value = new Credential() {Username = Guid.NewGuid().ToString(), Password = Guid.NewGuid().ToString()};

            var tokenReturn = new AccessTokenResponse("access-token", 3600, tokenType: "bearer");
            _tokensApi.Token("client_credentials", clientId: _devOpsContext.ClientId,
                    clientSecret: _devOpsContext.ClientSecret)
                .Returns(tokenReturn);

            var secretReturn = Task.FromResult(new SecretResponse(path: oldSecretPath));
            _secretsApi.UpdateSecretAsync(oldSecretPath,
                    Arg.Is<SecretUpsert>(x => x.Data["password"].Equals(value.Password) && x.Data["username"].Equals(value.Username)))
                .Returns(secretReturn);

            var result = await _secureStore.UpdateCredentialsAsync(_ctxString, secretPath, oldSecretPath, value);
            
            Assert.AreEqual(oldSecretPath, result);
        }
    }
}