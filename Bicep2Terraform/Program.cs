using System;
using Azure.Deployments.Core.Json;
using Bicep.Core.Navigation;
using Bicep.Core.Parsing;
using Bicep.Core.Syntax;

namespace Bicep2Terraform
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var content = @"
@minLength(3)
@maxLength(11)
param storagePrefix string

param storageSKU string = 'Standard_LRS'
param location string = resourceGroup().location

var uniqueStorageName = '${storagePrefix}${uniqueString(resourceGroup().id)}'

resource stg 'Microsoft.Storage/storageAccounts@2019-04-01' = {
  name: uniqueStorageName
  location: location
  sku: {
    name: storageSKU
  }
  kind: 'StorageV2'
  properties: {
    supportsHttpsTrafficOnly: true
  }
}

module webModule './webApp.bicep' = {
  name: 'webDeploy'
  params: {
    skuName: 'S1'
    location: location
  }
}

output storageEndpoint object = stg.properties.primaryEndpoints
";

            content = @"
@description('Specifies the location of AKS cluster.')
param location string = resourceGroup().location

@description('Specifies the name of the AKS cluster.')
param aksClusterName string = 'aks-${uniqueString(resourceGroup().id)}'

@description('Specifies the DNS prefix specified when creating the managed cluster.')
param aksClusterDnsPrefix string = aksClusterName

@description('Specifies the tags of the AKS cluster.')
param aksClusterTags object = {
  resourceType: 'AKS Cluster'
  createdBy: 'ARM Template'
}

@allowed([
  'azure'
  'kubenet'
])
@description('Specifies the network plugin used for building Kubernetes network. - azure or kubenet.')
param aksClusterNetworkPlugin string = 'azure'

@allowed([
  'azure'
  'calico'
])
@description('Specifies the network policy used for building Kubernetes network. - calico or azure')
param aksClusterNetworkPolicy string = 'azure'

@description('Specifies the CIDR notation IP range from which to assign pod IPs when kubenet is used.')
param aksClusterPodCidr string = '10.244.0.0/16'

@description('A CIDR notation IP range from which to assign service cluster IPs. It must not overlap with any Subnet IP ranges.')
param aksClusterServiceCidr string = '10.2.0.0/16'

@description('Specifies the IP address assigned to the Kubernetes DNS service. It must be within the Kubernetes service address range specified in serviceCidr.')
param aksClusterDnsServiceIP string = '10.2.0.10'

@description('Specifies the CIDR notation IP range assigned to the Docker bridge network. It must not overlap with any Subnet IP ranges or the Kubernetes service address range.')
param aksClusterDockerBridgeCidr string = '172.17.0.1/16'

@allowed([
  'basic'
  'standard'
])
@description('Specifies the sku of the load balancer used by the virtual machine scale sets used by nodepools.')
param aksClusterLoadBalancerSku string = 'standard'

@allowed([
  'Paid'
  'Free'
])
@description('Specifies the tier of a managed cluster SKU: Paid or Free')
param aksClusterSkuTier string = 'Paid'

@description('Specifies the version of Kubernetes specified when creating the managed cluster.')
param aksClusterKubernetesVersion string = '1.19.7'

@description('Specifies the administrator username of Linux virtual machines.')
param aksClusterAdminUsername string

@description('Specifies the SSH RSA public key string for the Linux nodes.')
param aksClusterSshPublicKey string

@description('Specifies whether enabling AAD integration.')
param aadEnabled bool = false

@description('Specifies the tenant id of the Azure Active Directory used by the AKS cluster for authentication.')
param aadProfileTenantId string = subscription().tenantId

@description('Specifies the AAD group object IDs that will have admin role of the cluster.')
param aadProfileAdminGroupObjectIDs array = []

@description('Specifies whether to create the cluster as a private cluster or not.')
param aksClusterEnablePrivateCluster bool = true

@description('Specifies whether to enable managed AAD integration.')
param aadProfileManaged bool = false

@description('Specifies whether to  to enable Azure RBAC for Kubernetes authorization.')
param aadProfileEnableAzureRBAC bool = false

@description('Specifies the unique name of the node pool profile in the context of the subscription and resource group.')
param nodePoolName string = 'nodepool1'

@description('Specifies the vm size of nodes in the node pool.')
param nodePoolVmSize string = 'Standard_DS3_v2'

@description('Specifies the OS Disk Size in GB to be used to specify the disk size for every machine in this master/agent pool. If you specify 0, it will apply the default osDisk size according to the vmSize specified..')
param nodePoolOsDiskSizeGB int = 100

@description('Specifies the number of agents (VMs) to host docker containers. Allowed values must be in the range of 1 to 100 (inclusive). The default value is 1.')
param nodePoolCount int = 3

@allowed([
  'Linux'
  'Windows'
])
@description('Specifies the OS type for the vms in the node pool. Choose from Linux and Windows. Default to Linux.')
param nodePoolOsType string = 'Linux'

@description('Specifies the maximum number of pods that can run on a node. The maximum number of pods per node in an AKS cluster is 250. The default maximum number of pods per node varies between kubenet and Azure CNI networking, and the method of cluster deployment.')
param nodePoolMaxPods int = 30

@description('Specifies the maximum number of nodes for auto-scaling for the node pool.')
param nodePoolMaxCount int = 5

@description('Specifies the minimum number of nodes for auto-scaling for the node pool.')
param nodePoolMinCount int = 3

@description('Specifies whether to enable auto-scaling for the node pool.')
param nodePoolEnableAutoScaling bool = true

@allowed([
  'Spot'
  'Regular'
])
@description('Specifies the virtual machine scale set priority: Spot or Regular.')
param nodePoolScaleSetPriority string = 'Regular'

@description('Specifies the Agent pool node labels to be persisted across all nodes in agent pool.')
param nodePoolNodeLabels object = {}

@description('Specifies the taints added to new nodes during node pool create and scale. For example, key=value:NoSchedule. - string')
param nodePoolNodeTaints array = []

@allowed([
  'System'
  'User'
])
@description('Specifies the mode of an agent pool: System or User')
param nodePoolMode string = 'System'

@allowed([
  'VirtualMachineScaleSets'
  'AvailabilitySet'
])
@description('Specifies the type of a node pool: VirtualMachineScaleSets or AvailabilitySet')
param nodePoolType string = 'VirtualMachineScaleSets'

@description('Specifies the availability zones for nodes. Requirese the use of VirtualMachineScaleSets as node pool type.')
param nodePoolAvailabilityZones array = []

@description('Specifies the id of the virtual network.')
param virtualNetworkId string

@description('Specifies the name of the default subnet hosting the AKS cluster.')
param aksSubnetName string = 'AksSubnet'

@description('Specifies the name of the Log Analytics Workspace.')
param logAnalyticsWorkspaceId string

var aadProfileConfiguration = {
  managed: aadProfileManaged
  enableAzureRBAC: aadProfileEnableAzureRBAC
  adminGroupObjectIDs: aadProfileAdminGroupObjectIDs
  tenantID: aadProfileTenantId
}

var virtualNetworkName = last(split(virtualNetworkId, '/'))

resource virtualNetwork 'Microsoft.Network/virtualNetworks@2020-08-01' existing = {
  name: virtualNetworkName
}

resource aksSubnet 'Microsoft.Network/virtualNetworks/subnets@2020-08-01' existing = {
  parent: virtualNetwork
  name: aksSubnetName
}

resource aksCluster 'Microsoft.ContainerService/managedClusters@2021-02-01' = {
  name: aksClusterName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  tags: aksClusterTags
  sku: {
    name: 'Basic'
    tier: aksClusterSkuTier
  }
  properties: {
    kubernetesVersion: aksClusterKubernetesVersion
    dnsPrefix: aksClusterDnsPrefix
    //TODO nodeResourceGroup: ''
    agentPoolProfiles: [
      {
        name: toLower(nodePoolName)
        count: nodePoolCount
        vmSize: nodePoolVmSize
        osDiskSizeGB: nodePoolOsDiskSizeGB
        vnetSubnetID: aksSubnet.id
        maxPods: nodePoolMaxPods
        osType: nodePoolOsType
        maxCount: nodePoolMaxCount
        minCount: nodePoolMinCount
        scaleSetPriority: nodePoolScaleSetPriority
        enableAutoScaling: nodePoolEnableAutoScaling
        mode: nodePoolMode
        type: nodePoolType
        availabilityZones: any(empty(nodePoolAvailabilityZones) ? null : nodePoolAvailabilityZones)
        nodeLabels: nodePoolNodeLabels
        nodeTaints: nodePoolNodeTaints
      }
    ]
    linuxProfile: {
      adminUsername: aksClusterAdminUsername
      ssh: {
        publicKeys: [
          {
            keyData: aksClusterSshPublicKey
          }
        ]
      }
    }
    addonProfiles: {
      omsagent: {
        enabled: true
        config: {
          logAnalyticsWorkspaceResourceID: logAnalyticsWorkspaceId
        }
      }
    }
    enableRBAC: true
    networkProfile: {
      networkPlugin: aksClusterNetworkPlugin
      networkPolicy: aksClusterNetworkPolicy
      podCidr: aksClusterPodCidr
      serviceCidr: aksClusterServiceCidr
      dnsServiceIP: aksClusterDnsServiceIP
      dockerBridgeCidr: aksClusterDockerBridgeCidr
      loadBalancerSku: aksClusterLoadBalancerSku
    }
    aadProfile: (aadEnabled ? aadProfileConfiguration : null)
    apiServerAccessProfile: {
      enablePrivateCluster: aksClusterEnablePrivateCluster
    }
  }
}
";

            var content1 = @"
@description('Location of the automation account')
param location string = resourceGroup().location

@description('Automation account name')
param name string

@description('Automation account sku')
@allowed([
  'Free'
  'Basic'
])
param sku string = 'Basic'

@description('Modules to import into automation account')
@metadata({
  name: 'Module name'
  version: 'Module version or specify latest to get the latest version'
  uri: 'Module package uri, e.g. https://www.powershellgallery.com/api/v2/package'
})
param modules array = []

@description('Runbooks to import into automation account')
@metadata({
  runbookName: 'Runbook name'
  runbookUri: 'Runbook URI'
  runbookType: 'Runbook type: Graph, Graph PowerShell, Graph PowerShellWorkflow, PowerShell, PowerShell Workflow, Script'
  logProgress: 'Enable progress logs'
  logVerbose: 'Enable verbose logs'
})
param runbooks array = []

@description('Enable delete lock')
param enableDeleteLock bool = false

@description('Enable diagnostic logs')
param enableDiagnostics bool = false

@description('Storage account name. Only required if enableDiagnostics is set to true.')
param diagnosticStorageAccountName string = ''

@description('Storage account resource group. Only required if enableDiagnostics is set to true.')
param diagnosticStorageAccountResourceGroup string = ''

@description('Log analytics workspace name. Only required if enableDiagnostics is set to true.')
param logAnalyticsWorkspaceName string = ''

@description('Log analytics workspace resource group. Only required if enableDiagnostics is set to true.')
param logAnalyticsResourceGroup string = ''

@description('Log analytics workspace subscription id (if differs from current subscription). Only required if enableDiagnostics is set to true.')
param logAnalyticsSubscriptionId string = subscription().subscriptionId

var lockName = '${automationAccount.name}-lck'
var diagnosticsName = '${diagnostics.properties.publishContentLink.uri}-dgs'

resource automationAccount 'Microsoft.Automation/automationAccounts@2020-01-13-preview' = {
  name: name
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    sku: {
      name: sku
    }
  }
}

resource automationAccountModules 'Microsoft.Automation/automationAccounts/modules@2020-01-13-preview' = [for module in modules: {
  parent: automationAccount
  name: module.name
  properties: {
    contentLink: {
      uri: module.version == 'latest' ? '${module.uri}/${module.name}' : '${module.uri}/${module.name}/${module.version}'
      version: module.version == 'latest' ? null : module.version
    }
  }
}]

resource runbook 'Microsoft.Automation/automationAccounts/runbooks@2019-06-01' = [for runbook in runbooks: {
  parent: automationAccount
  name: runbook.runbookName
  location: location
  properties: {
    runbookType: runbook.runbookType
    logProgress: runbook.logProgress
    logVerbose: runbook.logVerbose
    publishContentLink: {
      uri: runbook.runbookUri
    }
  }
}]

resource lock 'Microsoft.Authorization/locks@2016-09-01' = if (enableDeleteLock) {
  scope: automationAccount

  name: lockName
  properties: {
    level: 'CanNotDelete'
  }
}

resource diagnostics 'microsoft.insights/diagnosticSettings@2017-05-01-preview' = if (enableDiagnostics) {
  scope: automationAccount

  name: diagnosticsName
  properties: {
    workspaceId: resourceId(logAnalyticsSubscriptionId, logAnalyticsResourceGroup, 'Microsoft.OperationalInsights/workspaces', logAnalyticsWorkspaceName)
    storageAccountId: resourceId(diagnosticStorageAccountResourceGroup, 'Microsoft.Storage/storageAccounts', diagnosticStorageAccountName)
    logs: [
      {
        category: 'JobLogs'
        enabled: true
      }
      {
        category: 'JobStreams'
        enabled: true
      }
      {
        category: 'DscNodeStatus'
        enabled: true
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
      }
    ]
  }
}

output systemIdentityPrincipalId string = automationAccount.identity.principalId
";


            Console.WriteLine(Converter.ToHcl(content));


        }
    }
}
