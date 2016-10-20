using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using TKInterfaces;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;


namespace TKStateless {
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class TKStateless : StatelessService, ITKStateless {
        public TKStateless(StatelessServiceContext context)
            : base(context) { }

        Dictionary<string, int> m_dict = new Dictionary<string, int>();
        public Task<int> GetAsync(string name) {
            int v=-1;
            m_dict.TryGetValue(name,out v);
            return Task.FromResult(v); 
        }
        public Task<int> SetAsync(string name, int value) {
            m_dict[name] = value;
            return Task.FromResult(value);
        }

        public Task<string> GetInfoAsync() {
            return Task.FromResult($"Context.InstanceId: {this.Context.InstanceId}, Context.NodeContext.NodeName: {this.Context.NodeContext.NodeName}, Context.PartitionId:{this.Context.PartitionId}, Context.ServiceName: {this.Context.ServiceName}");
        }



        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners() {
            return new[] { new ServiceInstanceListener(context =>
            this.CreateServiceRemotingListener(context)) };

            //Or implement WcfCommunicationClientFactory etc.
        }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken) {
            // TODO: Replace the following sample code with your own logic 
            //       or remove this RunAsync override if it's not needed in your service.

            long iterations = 0;

            while (true) {
                cancellationToken.ThrowIfCancellationRequested();

                ServiceEventSource.Current.ServiceMessage(this, "Working-{0}", ++iterations);

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }
}
