using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors;
using TKInterfaces;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Remoting;
using System.Web.Http;
using Microsoft.ServiceFabric.Actors.Query;
using System.Fabric;
using Microsoft.ServiceFabric.Data;
using System.Threading;
using TKWebApi;
using System.Fabric.Description;
using System.Fabric.Health;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace TKWeb.Controllers
{
    [RoutePrefix("api/sf")]
    public class SFController : ApiController
    {
        //[HttpGet]
        //public IEnumerable<string> Get()
        //{
        //    return new string[] { "value1", "value2" };
        //}

        /// Yes, I know, GET for changing state is...
        /// But easier to show!
        ///localhost:8080/api/sf/ActorSet?actorId=2&value=2
        ///localhost:8080/api/sf/ActorGet?actorId=2
        ///localhost:8080/api/sf/ActorGetInfo?actorId=2
        ///localhost:8080/api/sf/ActorGetAll
        ///http://pltkw3sfcluster.westeurope.cloudapp.azure.com:8080/api/sf/ActorSet?actorId=2&value=2
        ///http://pltkw3sfcluster.westeurope.cloudapp.azure.com:8080/api/sf/ActorGet?actorId=2
        ///http://pltkw3sfcluster.westeurope.cloudapp.azure.com:8080/api/sf/TKStatefulSet?partition=3&name=abc&value=3
        ///http://pltkw3sfcluster.westeurope.cloudapp.azure.com:8080/api/sf/TKStatefulSet?partition=1&name=abc&value=3
        ///http://pltkw3sfcluster.westeurope.cloudapp.azure.com:8080/api/sf/TKStatefulGet?partition=1&name=abc
        ///http://pltkw3sfcluster.westeurope.cloudapp.azure.com:8080/api/sf/TKStatelessGetInfo?partition=1
        ///http://pltkw3sfcluster.westeurope.cloudapp.azure.com:8080/api/sf/TKStatelessGetInfo?partition=2
        ///http://pltkw3sfcluster.westeurope.cloudapp.azure.com:8080/api/sf/TKStatelessGetInfo?partition=-1
        ///http://pltkw3sfcluster.westeurope.cloudapp.azure.com:8080/api/sf/TKStatelessSet?partition=-1&name=abc&value=3
        ///http://pltkw3sfcluster.westeurope.cloudapp.azure.com:8080/api/sf/TKStatelessSet?partition=2&name=abc&value=3
        ///http://pltkw3sfcluster.westeurope.cloudapp.azure.com:8080/api/sf/TKStatelessGet?partition=2&name=abc
        ///http://pltkw3sfcluster.westeurope.cloudapp.azure.com:8080/api/sf/TKStatelessGet?partition=1&name=abc
        ///http://pltkw3sfcluster.westeurope.cloudapp.azure.com:8080/api/sf/TKStatelessGet?partition=-1&name=abc
        /// </remarks>

        #region Actors
        [HttpGet]
        [Route("ActorSet")]
        public async Task<string> ActorSet(long actorId, int value) {
            var start = DateTime.Now;
            var actor = ActorProxy.Create<ITKActor>(new ActorId(actorId), new Uri("fabric:/TK_2016MainSFFunctions/TKActorService"));
            await actor.SetCountAsync(value);
            var stop = DateTime.Now;
            return $"Actor: {actorId}, {(DateTime.Now - start).TotalMilliseconds}, {value}";
        }

        [HttpGet]
        [Route("ActorGet")]
        public async Task<string> ActorGet(long actorId) {
            var start = DateTime.Now;
            var actor = ActorProxy.Create<ITKActor>(new ActorId(actorId), new Uri("fabric:/TK_2016MainSFFunctions/TKActorService"));
            var value = await actor.GetCountAsync();
            var stop = DateTime.Now;
            return $"Actor: {actorId}, {(DateTime.Now - start).TotalMilliseconds}, {value}";
        }

        [HttpGet]
        [Route("ActorGetInfo")]
        public async Task<string> ActorGetInfo(long actorId) {
            var start = DateTime.Now;
            var actor = ActorProxy.Create<ITKActor>(new ActorId(actorId), new Uri("fabric:/TK_2016MainSFFunctions/TKActorService"));
            var value = await actor.GetInfo();
            var stop = DateTime.Now;
            return $"Actor: {actorId}, {(DateTime.Now - start).TotalMilliseconds}, {value}";
        }

        #endregion
        #region Statefull
        [HttpGet]
        [Route("TKStatefulSet")]
        public async Task<string> TKStatefulSet(int partition, string name, int value) {
            var start = DateTime.Now;
            try {
                var proxy = ServiceProxy.Create<ITKStateful>(new Uri("fabric:/TK_2016MainSFFunctions/TKStateful"),
                    new Microsoft.ServiceFabric.Services.Client.ServicePartitionKey(partition),
                    TargetReplicaSelector.Default);
                //Can switch replicas here! Due to failover
                await proxy.SetAsync(name, value);
            } catch (Exception ex) {
                return $"Primary replica was changed - {ex.ToString()}";

            }
            var stop = DateTime.Now;
            return $"Partition: {partition}, {(DateTime.Now - start).TotalMilliseconds}, {value}";
        }
        [HttpGet]
        [Route("TKStatefulGet")]
        public async Task<string> TKStatefulGet(int partition, string name) {
            var start = DateTime.Now;
            var proxy = ServiceProxy.Create<ITKStateful>(new Uri("fabric:/TK_2016MainSFFunctions/TKStateful"),
                new Microsoft.ServiceFabric.Services.Client.ServicePartitionKey(partition));
            var value = await proxy.GetAsync(name);
            var stop = DateTime.Now;
            return $"Partition: {partition}, {(DateTime.Now - start).TotalMilliseconds}, {value}";
        }
        [HttpGet]
        [Route("TKStatefulGetInfo")]
        public async Task<string> TKStatefulGetInfo(int partition) {
            var start = DateTime.Now;
            var proxy = ServiceProxy.Create<ITKStateful>(new Uri("fabric:/TK_2016MainSFFunctions/TKStateful"),
                new Microsoft.ServiceFabric.Services.Client.ServicePartitionKey(partition));
            var value = await proxy.GetInfoAsync();
            var stop = DateTime.Now;
            return $"Partition: {partition}, {(DateTime.Now - start).TotalMilliseconds}, {value}";
        }
        

        #endregion
        #region Stateless
        [HttpGet]
        [Route("TKStatelessGetInfo")]
        public async Task<string> TKStatelessGetInfo(int partition) {
            var start = DateTime.Now;
            ITKStateless proxy = null;
            //For single partitions
            if (partition == -1) {
                proxy = ServiceProxy.Create<ITKStateless>(new Uri("fabric:/TK_2016MainSFFunctions/TKStateless")
                    );
            } else {
                proxy = ServiceProxy.Create<ITKStateless>(new Uri("fabric:/TK_2016MainSFFunctions/TKStateless")
                    , new Microsoft.ServiceFabric.Services.Client.ServicePartitionKey(partition));
            }
            var value = await proxy.GetInfoAsync();
            var stop = DateTime.Now;
            return $"Time: {(DateTime.Now - start).TotalMilliseconds}, {value}";

        }

        [HttpGet]
        [Route("TKStatelessGet")]
        public async Task<string> TKStatelessGet(int partition, string name) {
            var start = DateTime.Now;
            ITKStateless proxy = null;
            //For single partitions
            if (partition == -1) {
                proxy = ServiceProxy.Create<ITKStateless>(new Uri("fabric:/TK_2016MainSFFunctions/TKStateless")
                    );
            } else {
                proxy = ServiceProxy.Create<ITKStateless>(new Uri("fabric:/TK_2016MainSFFunctions/TKStateless")
                    , new Microsoft.ServiceFabric.Services.Client.ServicePartitionKey(partition));
            }
            var value = await proxy.GetAsync(name);
            var stop = DateTime.Now;
            return $"Partition: {partition}, {(DateTime.Now - start).TotalMilliseconds}, {value}";
        }

        [HttpGet]
        [Route("TKStatelessSet")]
        public async Task<string> TKStatelessSet(int partition, string name, int value) {
            var start = DateTime.Now;
            ITKStateless proxy = null;
            //For single partitions
            if (partition == -1) {
                proxy = ServiceProxy.Create<ITKStateless>(new Uri("fabric:/TK_2016MainSFFunctions/TKStateless")
                    );
            } else {
                proxy = ServiceProxy.Create<ITKStateless>(new Uri("fabric:/TK_2016MainSFFunctions/TKStateless")
                    , new Microsoft.ServiceFabric.Services.Client.ServicePartitionKey(partition));
            }
            var v = await proxy.SetAsync(name, value);
            var stop = DateTime.Now;
            return $"Partition: {partition}, {(DateTime.Now - start).TotalMilliseconds}, {v}";
        }
        #endregion
        #region Actors & Cluster Management
        [HttpGet]
        [Route("ActorGetAll")]
        public async Task<List<ActorInformation>> ActorGetAll() {
            List<ActorInformation> activeActors = new List<ActorInformation>();
            var client = new FabricClient();
            var partitions = await client.QueryManager.GetPartitionListAsync(new Uri("fabric:/TK_2016MainSFFunctions/TKActorService"));
            foreach (var partition in partitions) {
                IActorService actorServiceProxy = ActorServiceProxy.Create(
                    new Uri("fabric:/TK_2016MainSFFunctions/TKActorService"), ((Int64RangePartitionInformation)partition.PartitionInformation).LowKey);

                ContinuationToken continuationToken = null;

                do {
                    PagedResult<ActorInformation> page = await actorServiceProxy.GetActorsAsync(continuationToken, new System.Threading.CancellationToken());
                    activeActors.AddRange(page.Items/*.Where(x => x.IsActive)*/);
                    continuationToken = page.ContinuationToken;
                }
                while (continuationToken != null);
            }
            return activeActors;
        }

        [HttpGet]
        [Route("GetClusterInfo")]
        public async Task<TKClusterInfo> GetClusterInfo() {
            TKClusterInfo result = new TKClusterInfo();
            var client = new FabricClient();
            result.ApplicationManifest= await client.ApplicationManager.GetApplicationManifestAsync("TK_2016MainSFFunctionsType", "1.0.0");
            //client.ApplicationManager.MoveNextApplicationUpgradeDomainAsync
            result.ClusterManifest = await client.ClusterManager.GetClusterManifestAsync();
            //client.ClusterManager.RemoveNodeStateAsync
            //await client.FaultManager.MovePrimaryAsync
            result.ApplicationHealth = await client.HealthManager.GetApplicationHealthAsync(new Uri("fabric:/TK_2016MainSFFunctions"));
            result.ClusterHealth = await client.HealthManager.GetClusterHealthAsync();
            result.AppList = await client.QueryManager.GetApplicationListAsync();
            result.NodeList = await client.QueryManager.GetNodeListAsync();

            //client.ServiceManager.UpdateServiceAsync
            //client.ServiceManager.GetServiceManifestAsync()
            result.ServiceDescription = new List<System.Fabric.Description.ServiceDescription>();
            foreach (var item in result.ApplicationHealth.ServiceHealthStates) {
                result.ServiceDescription.Add(await client.ServiceManager.GetServiceDescriptionAsync(item.ServiceName));
            }

            return result;
        }

        #endregion
        #region Backup
        [HttpGet]
        [Route("DoBackup")]
        public void DoBackup(int partition) {
            var proxy = ServiceProxy.Create<ITKStateful>(new Uri("fabric:/TK_2016MainSFFunctions/TKStateful"),
                new Microsoft.ServiceFabric.Services.Client.ServicePartitionKey(partition),
                TargetReplicaSelector.Default);
            proxy.DoBackup();
        }
        #endregion
        #region Health
        [HttpGet]
        [Route("TKCustomHealthWarning")]
        public void TKCustomHealthWarning() {
            var activationContext = FabricRuntime.GetActivationContext();
            HealthInformation healthInformation =
                new HealthInformation("TKWebApi", "TKCustomHealth", HealthState.Warning);
            healthInformation.TimeToLive = TimeSpan.FromSeconds(15);
            activationContext.ReportApplicationHealth(healthInformation);
            //activationContext.ReportDeployedApplicationHealth
            //activationContext.ReportDeployedServicePackageHealth
        }
        [HttpGet]
        [Route("TKCustomHealthOK")]
        public void TKCustomHealthOK() {
            var activationContext = FabricRuntime.GetActivationContext();
            HealthInformation healthInformation =
                new HealthInformation("TKWebApi", "TKCustomHealth", HealthState.Ok);
            activationContext.ReportApplicationHealth(healthInformation);
        }
        #endregion


    }
}
