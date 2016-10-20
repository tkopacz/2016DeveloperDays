using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Web.Http;
using TKActorEventSource.Interfaces;

namespace TKWebApiEventClient.Controllers {
    /*
     * --Actor
public interface IGameEvents : IActorEvents {
    void GameScoreUpdated(Guid gameId, string currentScore); }

public interface IGameActor : IActor, IActorEventPublisher<IGameEvents>

--Actor: send
var ev = GetEvent<IGameEvents>();
ev.GameScoreUpdated(Id.GetGuidId(), State.Status.Score);

--Client (stateles Web etc)
class GameEventsHandler : IGameEvents /…/

var proxy = ActorProxy.Create<IGameActor>(
                    new ActorId(Guid.Parse(arg)), ApplicationName);
proxy.SubscribeAsync(new GameEventsHandler()).Wait();

Can failover to another replica!

     */


    internal class TKProgressEvents : ITKProgressEvents {
        public void ProgressUpdated(string message) {
            ServiceEventSource.Current.Message($"ProgressUpdated: {message}");
        }
    }

    [ServiceRequestActionFilter]
    public class ValuesController : ApiController {
        // GET api/values 
        public IEnumerable<string> Get() {
            return new string[] { "ABC" };

        }

        // GET api/values/5 
        public async Task<string> Get(int id) {
            try {
                ITKActorEventSource actor = ActorProxy.Create<ITKActorEventSource>
                    (new ActorId(id), "fabric:/TK_EventsAndService");
                await actor.SubscribeAsync(new TKProgressEvents());
                await actor.Test();
                var result = await actor.StartLongCalculationAsync(1000);
                return $"{id}";
            } catch (Exception ex) {
                Debug.Write(ex.ToString());
            }
            return "???";

        }

        // POST api/values 
        public void Post([FromBody]string value) {
        }

        // PUT api/values/5 
        public void Put(int id, [FromBody]string value) {
        }

        // DELETE api/values/5 
        public void Delete(int id) {
        }
    }
}
