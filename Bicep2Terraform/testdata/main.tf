//[INFO] Variables not allowed as default input value. All usage of `location` will be replaced with `azurerm_resource_group.test.location`

variable "name" {
  type        = string
  description = "Automation account name"
}

variable "sku" {
  type        = string
  default     = "Basic"
  description = "Automation account sku"
  validation {
    condition = contains([
      "Free",
      "Basic",
    ], var.sku)
    error_message = "Allowed values are ['Free','Basic']."
  }
}

variable "modules" {
  type = list(any)
  default = [
  ]
  description = "Modules to import into automation account"
}

variable "runbooks" {
  type = list(any)
  default = [
  ]
  description = "Runbooks to import into automation account"
}

variable "enableDeleteLock" {
  type        = bool
  default     = false
  description = "Enable delete lock"
}

variable "enableDiagnostics" {
  type        = bool
  default     = false
  description = "Enable diagnostic logs"
}

variable "diagnosticStorageAccountName" {
  type        = string
  default     = ""
  description = "Storage account name. Only required if enableDiagnostics is set to true."
}

variable "diagnosticStorageAccountResourceGroup" {
  type        = string
  default     = ""
  description = "Storage account resource group. Only required if enableDiagnostics is set to true."
}

variable "logAnalyticsWorkspaceName" {
  type        = string
  default     = ""
  description = "Log analytics workspace name. Only required if enableDiagnostics is set to true."
}

variable "logAnalyticsResourceGroup" {
  type        = string
  default     = ""
  description = "Log analytics workspace resource group. Only required if enableDiagnostics is set to true."
}

//[INFO] Variables not allowed as default input value. All usage of `logAnalyticsSubscriptionId` will be replaced with `data.azurerm_client_config.current.subscription_id`


locals {
  lockName = "${jsondecode(azurerm-restapi_resource.automationAccount.output).name}-lck"
}

locals {
  diagnosticsName = "${jsondecode(azurerm-restapi_resource.automationAccount.output).name}-dgs"
}


resource "azurerm-restapi_resource" "automationAccount" {
  resource_id            = "${azurerm_resource_group.test.id}/providers/Microsoft.Automation/automationAccounts/${var.name}"
  type                   = "Microsoft.Automation/automationAccounts@2020-01-13-preview"
  response_export_values = ["name", "identity"]

  location = azurerm_resource_group.test.location
  identity {
    type = "SystemAssigned"
  }

  body = jsonencode({
    properties = {
      sku = {
        name = var.sku
      }
    }
  })
}

resource "azurerm-restapi_resource" "automationAccountModules" {
  resource_id = "${azurerm-restapi_resource.automationAccount.resource_id}/modules/${each.value.name}"
  type        = "Microsoft.Automation/automationAccounts/modules@2020-01-13-preview"

  body = jsonencode({
    properties = {
      contentLink = {
        uri     = each.value.version == "latest" ? "${each.value.uri}/${each.value.name}" : "${each.value.uri}/${each.value.name}/${each.value.version}"
        version = each.value.version == "latest" ? null : each.value.version
      }
    }
  })
  for_each = { for i, v in var.modules : "item${i}" => v }
}

resource "azurerm-restapi_resource" "runbook" {
  resource_id = "${azurerm-restapi_resource.automationAccount.resource_id}/runbooks/${each.value.runbookName}"
  type        = "Microsoft.Automation/automationAccounts/runbooks@2019-06-01"

  location = azurerm_resource_group.test.location
  body = jsonencode({
    properties = {
      runbookType = each.value.runbookType
      logProgress = each.value.logProgress
      logVerbose  = each.value.logVerbose
      publishContentLink = {
        uri = each.value.runbookUri
      }
    }
  })
  for_each = { for i, v in var.runbooks : "item${i}" => v }
}

resource "azurerm-restapi_resource" "lock" {
  resource_id = "${azurerm-restapi_resource.automationAccount.resource_id}/Microsoft.Authorization/locks/${local.lockName}"
  type        = "Microsoft.Authorization/locks@2016-09-01"

  count = (var.enableDeleteLock) ? 1 : 0
  body = jsonencode({
    properties = {
      level = "CanNotDelete"
    }
  })
}

resource "azurerm-restapi_resource" "diagnostics" {
  resource_id = "${azurerm-restapi_resource.automationAccount.resource_id}/microsoft.insights/diagnosticSettings/${local.diagnosticsName}"
  type        = "microsoft.insights/diagnosticSettings@2017-05-01-preview"

  count = (var.enableDiagnostics) ? 1 : 0
  body = jsonencode({
    properties = {
      workspaceId      = "/subscriptions/${data.azurerm_client_config.current.subscription_id}/resourceGroups/${var.logAnalyticsResourceGroup}/providers/Microsoft.OperationalInsights/workspaces/${var.logAnalyticsWorkspaceName}"
      storageAccountId = "/subscriptions/${data.azurerm_client_config.current.subscription_id}/resourceGroups/${var.diagnosticStorageAccountResourceGroup}/providers/Microsoft.Storage/storageAccounts/${var.diagnosticStorageAccountName}"
      logs = [
        {
          category = "JobLogs"
          enabled  = true
        },
        {
          category = "JobStreams"
          enabled  = true
        },
        {
          category = "DscNodeStatus"
          enabled  = true
        },
      ]
      metrics = [
        {
          category = "AllMetrics"
          enabled  = true
        },
      ]
    }
  })
}


output "systemIdentityPrincipalId" {
  value = jsondecode(azurerm-restapi_resource.automationAccount.output).identity.principalId
}


