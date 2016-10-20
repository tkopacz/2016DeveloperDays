using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Timer1.Interfaces;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ApplicationInsights;

namespace StatefulServiceTimer
{

    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class StatefulServiceTimer : StatefulService
    {
        const string FABRIC_APP = "fabric:/TKActorSimpleTimer";
        private TelemetryClient m_tc;
        public StatefulServiceTimer(StatefulServiceContext context)
            : base(context)
        {

        }

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see http://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new ServiceReplicaListener[0];
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            m_tc = new TelemetryClient();
            m_tc.InstrumentationKey = "1dc0d5c5-f513-4539-8311-9c0d91f0ea14";
            m_tc.Context.Component.Version = "4.0";
            m_tc.Context.User.Id = "StatefulServiceTimer";
            m_tc.TrackEvent($"RunAsync");
            m_tc.Flush();

            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>("myDictionary");
            Random rnd = new Random();
            ITimer1 proxy = ActorProxy.Create<ITimer1>(new ActorId("P1"), FABRIC_APP);
            await proxy.RegisterTimerAsync(80 * 60); //Timer will be never call!
            proxy = ActorProxy.Create<ITimer1>(new ActorId("P2"), FABRIC_APP);
            await proxy.RegisterTimerAsync(1 * 60); //Timer will be call 3 or 4 times
            proxy = ActorProxy.Create<ITimer1>(new ActorId("P3"), FABRIC_APP);
            await proxy.RegisterTimerAsync(3 * 60); //Timer will be call once
        }
    }
}
