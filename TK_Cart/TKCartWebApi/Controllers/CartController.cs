using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;
using TKCart.Interfaces;

namespace TKCartWebApi.Controllers {

    #region WebApi Models
    public class SetCustomerInfoParams {
        public int ActorId { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
    }

    public class AddItemParams {
        public int ActorId { get; set; }
        public string Name { get; set; }
        public int Count { get; set; }
    }
    public class AddItemsParams {
        public int ActorId { get; set; }
        public IEnumerable<OrderLines> Lines { get; set;}
    }
    public class ShoppingCartReturnValue {
        public ShoppingCartReturnValue() {
            Name = Surname = "";
            Lines = new OrderLines[] { };
        }
        public ShoppingCartReturnValue(int orderId, string name, string surname, IEnumerable<OrderLines> lines, bool confirmed) {
            OrderId = orderId; Name = name; Surname = surname; Lines = lines;
            Confirmed = confirmed;
        }

        public int OrderId { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public IEnumerable<OrderLines> Lines { get; }
        public bool Confirmed { get; set; }
    }
    #endregion

    [ServiceRequestActionFilter]
    [RoutePrefix("api/cart")]
    public class CartController : ApiController {

        private readonly Uri m_actorUri = new Uri("fabric:/TK_Cart/TKCartActorService");

        [HttpPost]
        [Route("SetCustomerInfo")]
        public async Task SetCustomerInfo(SetCustomerInfoParams data) {
            var actor = ActorProxy.Create<ITKCart>(new ActorId(data.ActorId), m_actorUri);
            await actor.SetCustomerInfo(data.Name, data.Surname);
        }
        [HttpPost]
        [Route("AddItem")]
        public async Task<int> AddItem(AddItemParams data) {
            var actor = ActorProxy.Create<ITKCart>(new ActorId(data.ActorId), m_actorUri);
            return await actor.AddItem(data.Name, data.Count);
        }
        [HttpPost]
        [Route("AddItems")]
        public async Task<int> AddItems(AddItemsParams data) {
            var actor = ActorProxy.Create<ITKCart>(new ActorId(data.ActorId), m_actorUri);
            return await actor.AddItems(data.Lines);
        }
        [HttpGet]
        [Route("GetShoppingCart")]
        public async Task<ShoppingCartReturnValue> GetShoppingCart(int actorId) {
            var actor = ActorProxy.Create<ITKCart>(new ActorId(actorId), m_actorUri);
            var sc= await actor.GetShoppingCart();
            return new ShoppingCartReturnValue(actorId, sc.Name, sc.Surname, sc.Lines,sc.Confirmed);
        }

        [HttpGet]
        [Route("Confirm")]
        public async Task Confirm(int actorId) {
            var actor = ActorProxy.Create<ITKCart>(new ActorId(actorId), m_actorUri);
            await actor.Confirm();
        }

        [HttpGet]
        [Route("ConfirmToSql")]
        public async Task ConfirmToSql(int actorId) {
            var actor = ActorProxy.Create<ITKCart>(new ActorId(actorId), m_actorUri);
            await actor.ConfirmToSql();
        }

        [HttpGet]
        [Route("ConfirmToQueue")]
        public async Task ConfirmToQueue(int actorId) {
            var actor = ActorProxy.Create<ITKCart>(new ActorId(actorId), m_actorUri);
            await actor.ConfirmToQueue();
        }
    }
}
