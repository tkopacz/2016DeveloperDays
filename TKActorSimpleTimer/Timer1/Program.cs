using System;
using System.Diagnostics;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace Timer1
{
    internal static class Program
    {
        /// <summary>
        /// This is the entry point of the service host process.
        /// </summary>
        private static void Main()
        {
            try
            {
                // This line registers an Actor Service to host your actor class with the Service Fabric runtime.
                // The contents of your ServiceManifest.xml and ApplicationManifest.xml files
                // are automatically populated when you build this project.
                // For more information, see http://aka.ms/servicefabricactorsplatform


                ActorRuntime.RegisterActorAsync<Timer1>(
                   (context, actorType) => new ActorService(context, actorType, 
                   null , null , null ,
                   new ActorServiceSettings()
                   {
                       ActorConcurrencySettings = new ActorConcurrencySettings()
                            { ReentrancyMode = ActorReentrancyMode.Disallowed },
                       ActorGarbageCollectionSettings =
                       //new ActorGarbageCollectionSettings(60 * 60, 60) //Default
                       new ActorGarbageCollectionSettings(5 * 60, 60) // 5 min IDLE, 1 min scan
                   }


                   )).GetAwaiter().GetResult();

                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception e)
            {
                ActorEventSource.Current.ActorHostInitializationFailed(e.ToString());
                throw;
            }
        }
    }
}
