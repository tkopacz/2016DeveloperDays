Connect-ServiceFabricCluster localhost:19000
Copy-ServiceFabricApplicationPackage C:\AzureFY15TK\05PaaS_ServiceFabric02RTM\TK_ETWAppInsightMetrics\TK_ETWAppInsightMetrics\pkg\Debug\ `
  -ImageStoreConnectionString file:C:\SfDevCluster\Data\ImageStoreShare -ApplicationPackagePathInImageStore TK_ETWAppInsightMetrics

Register-ServiceFabricApplicationType TK_ETWAppInsightMetrics 

Get-ServiceFabricApplicationType

New-ServiceFabricApplication fabric:/TK_ETWAppInsightMetricsType TK_ETWAppInsightMetricsType 1.0.0

Get-ServiceFabricApplication

Get-ServiceFabricApplication | Get-ServiceFabricService

#Start-ServiceFabricApplicationUpgrade -ApplicationName fabric:/TK_ETWAppInsightMetricsType -ApplicationTypeVersion 2.0.0 -HealthCheckStableDurationSec 60 -UpgradeDomainTimeoutSec 1200 -UpgradeTimeout 3000  -FailureAction Rollback -Monitored
#Remove-ServiceFabricApplication fabric:/TK_ETWAppInsightMetricsType -Force
#Unregister-ServiceFabricApplicationType TK_ETWAppInsightMetricsType -ApplicationTypeVersion 1.0.0 -Force

New-ServiceFabricService -ApplicationName fabric:/TK_ETWAppInsightMetricsType -Ser