using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using TKCart.Interfaces;

namespace TKCartSaveToSQL.Interfaces {
    /// <summary>
    /// This interface defines the methods exposed by an actor.
    /// Clients use this interface to interact with the actor that implements it.
    /// </summary>
    public interface ITKCartSaveToSQL : IActor {
        Task<bool> AddToSave(ShoppingCart cart);
        Task Save();
    }
}
