using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Actors.Client;
using TKCartSaveToSQL.Interfaces;
using TKCart.Interfaces;

namespace TKCartSaveToSQL {
    /// <remarks>
    /// This class represents an actor.
    /// Every ActorID maps to an instance of this class.
    /// The StatePersistence attribute determines persistence and replication of actor state:
    ///  - Persisted: State is written to disk and replicated.
    ///  - Volatile: State is kept in memory only and replicated.
    ///  - None: State is kept in memory only and not replicated.
    /// </remarks>
    [StatePersistence(StatePersistence.Persisted)]
    internal class TKCartSaveToSQL : Actor, ITKCartSaveToSQL {
        /// <summary>
        /// Initializes a new instance of TKCartSaveToSQL
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        public TKCartSaveToSQL(ActorService actorService, ActorId actorId)
            : base(actorService, actorId) {
        }

        public async Task<bool> AddToSave(ShoppingCart cart) {
            List<ShoppingCart> existings = await this.StateManager.GetStateAsync<List<ShoppingCart>>("state");
            List<ShoppingCart> l = new List<ShoppingCart>();
            l.AddRange(existings);
            l.Add(cart);
            await this.StateManager.SetStateAsync<List<ShoppingCart>>("state", l);
            if (l.Count > 10) await Save(); //Or something
            return true;
        }
        public async Task Save() {
            //Can be called by client, or by some watchdog etc.
            //See next sample - Timers, Reminders
            var lst = await this.StateManager.GetStateAsync<List<ShoppingCart>>("state");
            //Something - skipped, save to SQL 
            await Task.Delay(2000);
        }

        protected override async Task OnDeactivateAsync() {
            await Save();
            await base.OnDeactivateAsync();
        }
        /// <summary>
        /// This method is called whenever an actor is activated.
        /// An actor is activated the first time any of its methods are invoked.
        /// </summary>
        protected override Task OnActivateAsync() {
            ActorEventSource.Current.ActorMessage(this, "Actor activated.");

            // The StateManager is this actor's private state store.
            // Data stored in the StateManager will be replicated for high-availability for actors that use volatile or persisted state storage.
            // Any serializable object can be saved in the StateManager.
            // For more information, see https://aka.ms/servicefabricactorsstateserialization
            this.StateManager.TryAddStateAsync<List<ShoppingCart>>("state", new List<ShoppingCart>());

            return this.StateManager.TryAddStateAsync("counter", 0);
        }

    }
}
