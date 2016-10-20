using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ApplicationInsights;

namespace MiscTelemetry {
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class MiscTelemetry : StatefulService {
        private TelemetryClient m_tc;

        public MiscTelemetry(StatefulServiceContext context)
            : base(context) {
            m_tc = new TelemetryClient();
            m_tc.InstrumentationKey = "1dc0d5c5-f513-4539-8311-9c0d91f0ea14";
            m_tc.Context.Component.Version = "1.0";
            m_tc.Context.User.Id = "MiscTelemetry";
            m_tc.TrackEvent($"MiscTelemetry");
            m_tc.Flush();

        }

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners() {
            return new ServiceReplicaListener[0];
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken) {
            // TODO: Replace the following sample code with your own logic 
            //       or remove this RunAsync override if it's not needed in your service.

            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>("myDictionary");
            Random rnd = new Random();
            int cnt = 0;
            while (true) {
                cancellationToken.ThrowIfCancellationRequested();

                using (var tx = this.StateManager.CreateTransaction()) {
                    var result = await myDictionary.TryGetValueAsync(tx, "Counter");

                    ServiceEventSource.Current.ServiceMessage(this, "Current Counter Value: {0}",
                        result.HasValue ? result.Value.ToString() : "Value does not exist.");

                    await myDictionary.AddOrUpdateAsync(tx, "Counter", 0, (key, value) => ++value);

                    // If an exception is thrown before calling CommitAsync, the transaction aborts, all changes are 
                    // discarded, and nothing is saved to the secondary replicas.
                    var n = rnd.NextDouble();
                    if (n>0.9) {
                        await tx.CommitAsync();
                        base.Partition.ReportMoveCost(MoveCost.High);
                    } else {
                        ServiceEventSource.Current.WarningRollback($"Because {n}");
                        cnt++;
                        base.Partition.ReportLoad(
                            new List<LoadMetric> {
                                new LoadMetric("BUSINESSOPCOUNT",cnt)
                            }

                            );
                        /*
                         * Register:
                         * New-ServiceFabricService -ApplicationName $applicationName -ServiceName $serviceName -ServiceTypeName $serviceTypeName –Stateful -MinReplicaSetSize 2 -TargetReplicaSetSize 3 -PartitionSchemeSingleton –Metric @("Memory,High,21,11”,"PrimaryCount,Medium,1,0”,"ReplicaCount,Low,1,1”,"Count,Low,1,1”)
                         * 
                         * -Metric @("ROLLBACK,Low,100,100")
                         */
                    }
                }

                ServiceEventSource.Current.Message("Msg1 - generic message"); //See difference with ServiceMessage!

                m_tc.TrackEvent($"Msg1 - generic message");
                m_tc.Flush();
                /*
                 * 
                 */
                //Can connect from inside to SF:
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }
}
