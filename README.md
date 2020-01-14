# Thycotic.UiPath.SecureStore
Credential Store plugins for UI Path orchestrator.  To use these credential store plugins, drop the corresponding dll into the plugins directory in the UI Path Orchestrator install directory and modify the web.config as specified here: https://github.com/UiPath/Orchestrator-CredentialStorePlugins


# DevOps Secrets Vault
The DevOps Secrets Vault (DSV) integration is read / write, so as assets are created and updated in UI Path Orchestrator they will get added to DSV.

## Setup
* In your DSV tenant create a new client credential assigned a role that has read / write / update / delete access to the base path where UI Path secrets will be stored.
  * https://docs.thycotic.com/dsv/1.0.0/cli-ref/client.md
* Go to Credential Stores in UI Path and add a new DevOpsSecretsVault credential store
  * DevOps Secrets Vault URL - your tenant url, i.e. https://tenantname.secretsvaultcloud.com
  * Client Id - the clientid from the client credential created earlier
  * Client Secret - the client secret for that client credential
  * Base Path Prefix - prefix that UI Path will append to the asset and robot lookups when getting credentials from DSV. For example if your prefix is `uipath/prod` then all secrets created will be under `/secrets/uipath/prod/<credentialname>` in DSV.


# Development
To build the plugin DLL's open the solution in Visual Studio and build the project in release mode. Release builds use ILRepack to bundle the dependent libraries into a single file to avoid version conflicts when Orchestrator loads the DLL. This is defined as a target in DevOpsVault.SecureStore .csproj file.
