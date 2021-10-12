// WVD Host Pool
resource /*${1:hostPool}*/hostPool 'Microsoft.DesktopVirtualization/hostpools@2021-07-12' = {
  name: /*${2:'name'}*/'name'
  location: resourceGroup().location
  properties: {
    friendlyName: /*${3:'hostpoolFriendlyName'}*/'hostpoolFriendlyName'
    hostPoolType: /*${4|'Personal','Pooled'|}*/'Personal'
    loadBalancerType: /*${5|'BreadthFirst','DepthFirst','Persistent'|}*/'BreadthFirst'
    preferredAppGroupType: /*${6|'Desktop','RailApplications','None'|}*/'Desktop'
  }
}
