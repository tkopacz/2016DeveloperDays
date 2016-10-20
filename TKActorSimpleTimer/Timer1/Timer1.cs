using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Actors.Client;
using Timer1.Interfaces;
using Microsoft.ApplicationInsights;

namespace Timer1
{
    /// <remarks>
    /// This class represents an actor.
    /// Every ActorID maps to an instance of this class.
    /// The StatePersistence attribute determines persistence and replication of actor state:
    ///  - Persisted: State is written to disk and replicated.
    ///  - Volatile: State is kept in memory only and replicated.
    ///  - None: State is kept in memory only and not replicated.
    /// </remarks>
    [StatePersistence(StatePersistence.Persisted)]
    internal sealed class Timer1 : Actor, ITimer1
    {
        /// <summary>
        /// Initializes a new instance of Actor1
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        public Timer1(ActorService actorService, ActorId actorId)
            : base(actorService, actorId) {
            m_tc = new TelemetryClient();
            m_tc.InstrumentationKey = "1dc0d5c5-f513-4539-8311-9c0d91f0ea14";
            m_tc.Context.Component.Version = "4.0";
            m_tc.Context.User.Id = actorId.ToString();
            m_tc.TrackEvent($"Timer1 - {actorId.ToString()}");
        }

        private IActorTimer m_WorkTimer;
        private TelemetryClient m_tc;
        private int m_delayInSecond;
        /// <summary>
        /// This method is called whenever an actor is activated.
        /// An actor is activated the first time any of its methods are invoked.
        /// </summary>
        protected override async Task OnActivateAsync()
        {
            ActorEventSource.Current.ActorMessage(this, "Actor activated.");

            // The StateManager is this actor's private state store.
            // Data stored in the StateManager will be replicated for high-availability for actors that use volatile or persisted state storage.
            // Any serializable object can be saved in the StateManager.
            // For more information, see http://aka.ms/servicefabricactorsstateserialization
            //Try
            var result = await this.StateManager.TryGetStateAsync<int>("m_delayInSecond");
            if (result.HasValue)
            {
                m_delayInSecond = result.Value;
                m_tc.TrackEvent($"OnActivateAsync A - {this.GetActorId().ToString()}, {m_delayInSecond}");
            }
            else
            {
                m_tc.TrackEvent($"OnActivateAsync B - {this.GetActorId().ToString()}");
            }

            m_tc.Flush();
            await this.StateManager.TryAddStateAsync("count", 0);
        }

        protected override Task OnDeactivateAsync()
        {
            if (m_WorkTimer != null)
            {
                UnregisterTimer(m_WorkTimer);
                m_WorkTimer = null;
            }
            m_tc.TrackEvent($"OnDeactivateAsync - {this.GetActorId().ToString()}, Timers Stop");
            m_tc.Flush();
            return base.OnDeactivateAsync();
        }

        public Task RegisterTimerAsync(int delayInSecond)
        {
            m_delayInSecond = delayInSecond;
            this.StateManager.SetStateAsync<int>("m_delayInSecond", m_delayInSecond);
            m_tc.TrackEvent($"RegisterTimerAsync - {this.GetActorId().ToString()}, {m_delayInSecond}");
            m_tc.Flush();
            if (m_WorkTimer != null)
            {
                UnregisterTimer(m_WorkTimer);
                m_WorkTimer = null;
            }

            m_WorkTimer = RegisterTimer(doWorkInTimerAsync, null, TimeSpan.FromSeconds(delayInSecond), TimeSpan.FromSeconds(delayInSecond));
            return Task.FromResult<int>(0);
        }
        private Task doWorkInTimerAsync(object arg)
        {
            m_tc.TrackEvent($"doWorkInTimerAsync {this.GetActorId().ToString()}, {m_delayInSecond}");
            m_tc.Flush();
            return Task.FromResult<int>(0);
        }

        /// <summary>
        /// TODO: Replace with your own actor method.
        /// </summary>
        /// <returns></returns>
        Task<int> ITimer1.GetCountAsync()
        {
            return this.StateManager.GetStateAsync<int>("count");
        }

        /// <summary>
        /// TODO: Replace with your own actor method.
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        Task ITimer1.SetCountAsync(int count)
        {
            // Requests are not guaranteed to be processed in order nor at most once.
            // The update function here verifies that the incoming count is greater than the current count to preserve order.
            return this.StateManager.AddOrUpdateStateAsync("count", count, (key, value) => count > value ? count : value);
        }
    }
}
