using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace QueueProcessingService {
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class QueueProcessingService : StatelessService {
        public QueueProcessingService(StatelessServiceContext context)
            : base(context) { }

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners() {
            return new ServiceInstanceListener[0];
        }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken) {
            //Setup
            NamespaceManager manager = NamespaceManager.CreateFromConnectionString("...");
            try {
                await manager.CreateQueueAsync("qtkcart2016");
            } catch (Exception ex) { }

            QueueClient queue = QueueClient.CreateFromConnectionString("...",
                "qtkcart2016");
            while(true) {
                var msg = await queue.ReceiveAsync();
                if (msg != null) {
                    //Do Something - save etc
                    ServiceEventSource.Current.Message($"{this.Context.InstanceId} - {msg.MessageId}");
                    await msg.CompleteAsync();

                }
            }

        }
    }
}
