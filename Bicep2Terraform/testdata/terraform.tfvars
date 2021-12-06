
name = "henglutest"
sku  = "Basic"
modules = [
  {
    name    = "mod1"
    version = "latest"
    uri     = "https://www.powershellgallery.com/api/v2/package"
  },
  {
    name    = "mod2"
    version = "latest"
    uri     = "https://www.powershellgallery.com/api/v2/package"
  },
]
runbooks = [
  {
    runbookName = "book1"
    runbookUri  = "https://www.powershellgallery.com/api/v2/package"
    runbookType = "Graph"
    logProgress = true
    logVerbose  = true
  },
  {
    runbookName = "book2"
    runbookUri  = "https://www.powershellgallery.com/api/v2/package"
    runbookType = "Graph"
    logProgress = true
    logVerbose  = true
  },
]
enableDeleteLock                      = true
enableDiagnostics                     = true
diagnosticStorageAccountName          = "diaghenglu"
diagnosticStorageAccountResourceGroup = "example-resource-group"
logAnalyticsWorkspaceName             = "loghenglu"
logAnalyticsResourceGroup             = "example-resource-group"