using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;

namespace TKActorEventSource.Interfaces {
    public interface ITKProgressEvents : IActorEvents {
        void ProgressUpdated(string message);
    }
    public interface ITKActorEventSource : IActor, IActorEventPublisher<ITKProgressEvents> {
        Task<int> StartLongCalculationAsync(int param);
        Task Test();
    }


}
