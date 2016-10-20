using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Reminder1.Interfaces;
using Microsoft.ApplicationInsights;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;

namespace ReminderService
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class ReminderService : StatefulService
    {
        const string FABRIC_APP = "fabric:/TKActorSimpleReminder";
        private TelemetryClient m_tc;

        public ReminderService(StatefulServiceContext context)
            : base(context)
        { }

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
            // TODO: Replace the following sample code with your own logic 
            //       or remove this RunAsync override if it's not needed in your service.

            m_tc = new TelemetryClient();
            m_tc.InstrumentationKey = "1dc0d5c5-f513-4539-8311-9c0d91f0ea14";
            m_tc.Context.Component.Version = "Reminder 4.0";
            m_tc.Context.User.Id = "ReminderService (statefull)";
            m_tc.TrackEvent($"RunAsync");
            m_tc.Flush();

            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>("myDictionary");
            Random rnd = new Random();
            IReminder1 proxy = ActorProxy.Create<IReminder1>(new ActorId("PR1"), FABRIC_APP);
            await proxy.TKRegisterReminderAsync(80 * 60);
            proxy = ActorProxy.Create<IReminder1>(new ActorId("PR2"), FABRIC_APP);
            await proxy.TKRegisterReminderAsync(1 * 60);
            proxy = ActorProxy.Create<IReminder1>(new ActorId("PR3"), FABRIC_APP);
            await proxy.TKRegisterReminderAsync(3 * 60); 


        }
    }
}
