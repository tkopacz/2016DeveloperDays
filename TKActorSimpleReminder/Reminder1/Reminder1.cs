using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Actors.Client;
using Reminder1.Interfaces;
using Microsoft.ApplicationInsights;

namespace Reminder1
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
    internal class Reminder1 : Actor, IReminder1, IRemindable
    {
        private TelemetryClient m_tc;
        /// <summary>
        /// Initializes a new instance of Actor1
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        public Reminder1(ActorService actorService, ActorId actorId)
            : base(actorService, actorId) {
            m_tc = new TelemetryClient();
            m_tc.InstrumentationKey = "1dc0d5c5-f513-4539-8311-9c0d91f0ea14";
            m_tc.Context.Component.Version = "Reminder 4.0";
            m_tc.Context.User.Id = actorId.ToString();
            m_tc.TrackEvent($"Reminder1 - {actorId.ToString()}");
            m_tc.Flush();
        }
        /// <summary>
        /// This method is called whenever an actor is activated.
        /// An actor is activated the first time any of its methods are invoked.
        /// </summary>
        protected override Task OnActivateAsync()
        {
            ActorEventSource.Current.ActorMessage(this, "Actor activated.");

            // The StateManager is this actor's private state store.
            // Data stored in the StateManager will be replicated for high-availability for actors that use volatile or persisted state storage.
            // Any serializable object can be saved in the StateManager.
            // For more information, see http://aka.ms/servicefabricactorsstateserialization

            m_tc.TrackEvent($"OnActivateAsync - {this.GetActorId().ToString()}");
            m_tc.Flush();
            return this.StateManager.TryAddStateAsync("count", 0);
        }

        protected override Task OnDeactivateAsync()
        {
            m_tc.TrackEvent($"OnDeactivateAsync - {this.GetActorId().ToString()}");
            m_tc.Flush();
            return base.OnDeactivateAsync();
        }

        protected override Task OnPreActorMethodAsync(ActorMethodContext actorMethodContext)
        {
            return base.OnPreActorMethodAsync(actorMethodContext);
        }

        protected override Task OnPostActorMethodAsync(ActorMethodContext actorMethodContext)
        {
            return base.OnPostActorMethodAsync(actorMethodContext);
        }

        /// <summary>
        /// Called always, regarding GC status
        /// </summary>
        /// <param name="reminderName"></param>
        /// <param name="context"></param>
        /// <param name="dueTime"></param>
        /// <param name="period"></param>
        /// <returns></returns>
        public async Task ReceiveReminderAsync(string reminderName, byte[] context, TimeSpan dueTime, TimeSpan period)
        {
            int cnt = await this.GetCountAsync();
            await this.SetCountAsync(cnt + 1);

            m_tc.TrackEvent($"ReceiveReminderAsync - {this.GetActorId().ToString()}, {reminderName}, {cnt}");
            m_tc.Flush();

            switch (reminderName)
            {
                default:
                    break;
            }
        }

        public Task TKRegisterReminderAsync(int delayInSecond)
        {
            m_tc.TrackEvent($"TKRegisterReminderAsync - {this.GetActorId().ToString()}, {delayInSecond}");
            m_tc.Flush();
            return this.RegisterReminderAsync("MyReminder", new byte[] { 1,2,3}, TimeSpan.FromSeconds(delayInSecond), TimeSpan.FromSeconds(delayInSecond));
        }

        public Task<int> GetCountAsync()
        {
            return this.StateManager.GetStateAsync<int>("count");
        }

        public Task SetCountAsync(int count)
        {
            // Requests are not guaranteed to be processed in order nor at most once.
            // The update function here verifies that the incoming count is greater than the current count to preserve order.
            return this.StateManager.AddOrUpdateStateAsync("count", count, (key, value) => count > value ? count : value);
        }
    }
}
