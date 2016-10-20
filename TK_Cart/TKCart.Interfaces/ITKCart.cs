using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using System.Runtime.Serialization;
using System.Collections.Immutable;

namespace TKCart.Interfaces {
    [DataContract]
    public class OrderLines {
        public OrderLines(string name, int count) {
            Name = name; Count = count;
        }
        [DataMember] public readonly string Name;
        [DataMember] public readonly int Count;
    }
    [DataContract]
    public class ShoppingCart {

        public ShoppingCart() {
            Name = Surname = "";
            Lines = new OrderLines[] { };
        }
        public ShoppingCart(string name, string surname, IEnumerable<OrderLines> lines, bool confirmed = false) {
            Name = name; Surname = surname; Lines = lines?.ToImmutableArray();
            Confirmed = confirmed;
        }

        [DataMember] public readonly string Name;
        [DataMember] public readonly string Surname;
        [DataMember] public readonly bool Confirmed;

        [DataMember] public IEnumerable<OrderLines> Lines { get; private set; }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context) {
            // Convert the deserialized collection to an immutable collection
            Lines = Lines.ToImmutableArray<OrderLines>();
            //Immutable or use something like Automapper
            //Immutable protected from dev mistakes, but...
        }
    }

    public interface ITKCart : IActor {
        Task SetCustomerInfo(string name, string surname);
        Task<int> AddItem(string name, int count);
        Task<int> AddItems(IEnumerable<OrderLines> lines);
        Task<ShoppingCart> GetShoppingCart();

        /* Confirm, process order etc.
         * Strategies (some of)
         * Reliable Actor = persisten state = we don't need to save (maybe id somewere)
         * Save to SQL - but SEPARATE Actor. Statefull of course!
         * A'la CQRS/ES - using queue
         * Common mistake: save inside the same actor. Ok - not mistake but "clumsiness"
         */
        Task Confirm();
        Task ConfirmToSql();
        Task ConfirmToQueue();
        

    }
}
