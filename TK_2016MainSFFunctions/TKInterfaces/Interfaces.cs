using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Services.Remoting;

namespace TKInterfaces
{
    public interface ITKStateless: Microsoft.ServiceFabric.Services.Remoting.IService {
        Task<string> GetInfoAsync();
        Task<int> GetAsync(string name);
        Task<int> SetAsync(string name, int value);
    }
    public interface ITKStateful: Microsoft.ServiceFabric.Services.Remoting.IService {
        Task<int> GetAsync(string name);
        Task SetAsync(string name,int value);
        Task<string> GetInfoAsync();
        Task DoBackup();

    }

    /// <summary>
    /// This interface defines the methods exposed by an actor.
    /// Clients use this interface to interact with the actor that implements it.
    /// </summary>
    public interface ITKActor : IActor {
        /// <summary>
        /// TODO: Replace with your own actor method.
        /// </summary>
        /// <returns></returns>
        Task<int> GetCountAsync();

        /// <summary>
        /// TODO: Replace with your own actor method.
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        Task SetCountAsync(int count);

        Task<string> GetInfo();
    }


}
