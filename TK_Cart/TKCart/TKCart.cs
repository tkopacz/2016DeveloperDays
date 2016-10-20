using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Actors.Client;
using TKCart.Interfaces;
using TKCartSaveToSQL.Interfaces;
using Microsoft.ServiceBus.Messaging;

namespace TKCart {
    /// <remarks>
    /// This class represents an actor.
    /// Every ActorID maps to an instance of this class.
    /// The StatePersistence attribute determines persistence and replication of actor state:
    ///  - Persisted: State is written to disk and replicated.
    ///  - Volatile: State is kept in memory only and replicated.
    ///  - None: State is kept in memory only and not replicated.
    /// </remarks>
    [StatePersistence(StatePersistence.Persisted)]
    internal class TKCart : Actor, ITKCart {
        /// <summary>
        /// Initializes a new instance of TKCart
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        public TKCart(ActorService actorService, ActorId actorId)
            : base(actorService, actorId) {
        }

        public async Task<int> AddItem(string name, int count) {
            ShoppingCart sc = await this.StateManager.GetStateAsync<ShoppingCart>("state");
            if (sc.Confirmed) throw new ApplicationException();
            List<OrderLines> l = new List<OrderLines>();
            if (sc.Lines!=null) l.AddRange(sc.Lines);
            l.Add(new OrderLines(name, count));
            ShoppingCart nsc = new ShoppingCart(sc.Name,sc.Surname,l);
            await this.StateManager.SetStateAsync<ShoppingCart>("state", nsc);
            return l.Count;
        }

        public async Task<int> AddItems(IEnumerable<OrderLines> lines) {
            ShoppingCart sc = await this.StateManager.GetStateAsync<ShoppingCart>("state");
            if (sc.Confirmed) throw new ApplicationException();
            List<OrderLines> l = new List<OrderLines>();
            if (sc.Lines != null) l.AddRange(sc.Lines);
            l.AddRange(lines);
            ShoppingCart nsc = new ShoppingCart(sc.Name, sc.Surname, l);
            await this.StateManager.SetStateAsync<ShoppingCart>("state", nsc);
            return l.Count;
        }

        public async Task<ShoppingCart> GetShoppingCart() {
            ShoppingCart sc = await this.StateManager.GetStateAsync<ShoppingCart>("state");
            return sc;
        }

        public async Task SetCustomerInfo(string name, string surname) {
            ShoppingCart sc = await this.StateManager.GetStateAsync<ShoppingCart>("state");
            if (sc.Confirmed) throw new ApplicationException();
            ShoppingCart nsc = new ShoppingCart(name, surname, sc.Lines);
            await this.StateManager.SetStateAsync<ShoppingCart>("state",nsc);
        }


        public async Task Confirm() {
            ShoppingCart sc = await this.StateManager.GetStateAsync<ShoppingCart>("state");
            ShoppingCart nsc = new ShoppingCart(sc.Name, sc.Surname, sc.Lines,true);
            await this.StateManager.SetStateAsync<ShoppingCart>("state", nsc);
        }

        public async Task ConfirmToQueue() {
            await Confirm();
            QueueClient queue = QueueClient.CreateFromConnectionString("...",
                "qtkcart2016");
            ShoppingCart sc = await this.StateManager.GetStateAsync<ShoppingCart>("state");
            BrokeredMessage msg = new BrokeredMessage(sc);
            await queue.SendAsync(msg);
        }

        public async Task ConfirmToSql() {
            await Confirm();
            long id = this.GetActorId().GetLongId();
            //Simplest aggregation, like 
            id = id / 10; // Every 10 orders etc.
            var actor = ActorProxy.Create<ITKCartSaveToSQL>(new ActorId(id), new Uri("fabric:/TK_Cart/TKCartSaveToSQLActorService"));
            ShoppingCart sc = await this.StateManager.GetStateAsync<ShoppingCart>("state");
            await actor.AddToSave(sc);

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

            return this.StateManager.TryAddStateAsync<ShoppingCart>("state", new ShoppingCart());
        }


    }
}
