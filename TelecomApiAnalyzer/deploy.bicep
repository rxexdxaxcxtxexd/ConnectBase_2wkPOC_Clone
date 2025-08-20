@description('Name of the App Service')
param appServiceName string

@description('Location for all resources')
param location string = resourceGroup().location

@description('The SKU of App Service Plan')
@allowed([
  'F1'
  'B1'
  'B2'
  'B3'
  'S1'
  'S2'
  'S3'
  'P1v2'
  'P2v2'
  'P3v2'
])
param sku string = 'B1'

@description('Runtime stack')
param linuxFxVersion string = 'DOTNETCORE|6.0'

@description('API Client ID')
@secure()
param apiClientId string = ''

@description('API Client Secret')
@secure()
param apiClientSecret string = ''

@description('API Token Endpoint')
param apiTokenEndpoint string = 'https://optuswholesale.cpq.cloud.sap/oauth/token'

@description('API Base URL')
param apiBaseUrl string = 'https://optuswholesale.cpq.cloud.sap'

var appServicePlanName = '${appServiceName}-plan'
var applicationInsightsName = '${appServiceName}-insights'
var storageAccountName = toLower('${appServiceName}storage')

// Storage Account for document uploads
resource storageAccount 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
  }
}

// App Service Plan
resource appServicePlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: sku
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

// Application Insights
resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: applicationInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Request_Source: 'IbizaWebAppExtensionCreate'
  }
}

// App Service
resource appService 'Microsoft.Web/sites@2022-03-01' = {
  name: appServiceName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: linuxFxVersion
      alwaysOn: sku != 'F1'
      http20Enabled: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      appSettings: [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsights.properties.ConnectionString
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'
        }
        {
          name: 'ApiSettings__TokenEndpoint'
          value: apiTokenEndpoint
        }
        {
          name: 'ApiSettings__ClientId'
          value: apiClientId
        }
        {
          name: 'ApiSettings__ClientSecret'
          value: apiClientSecret
        }
        {
          name: 'ApiSettings__BaseUrl'
          value: apiBaseUrl
        }
        {
          name: 'AzureStorage__ConnectionString'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
      ]
    }
  }
}

// Configure source control deployment (optional)
resource sourceControl 'Microsoft.Web/sites/sourcecontrols@2022-03-01' = {
  parent: appService
  name: 'web'
  properties: {
    repoUrl: 'https://github.com/your-repo/telecom-api-analyzer'
    branch: 'main'
    isManualIntegration: true
  }
}

// Outputs
output appServiceUrl string = 'https://${appService.properties.defaultHostName}'
output appServicePrincipalId string = appService.identity.principalId
output storageAccountName string = storageAccount.name
output applicationInsightsInstrumentationKey string = applicationInsights.properties.InstrumentationKey