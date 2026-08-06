using Microsoft.AspNetCore.Mvc;

namespace MicroservicesTest.SenderApi.Modules.Orders
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController(OrderModule orderModule) : ControllerBase
    {


        [HttpPost(Name = "Order")]
        public Task SaveOrder(OrderDto order)
        {
            return orderModule.SaveOrder(order, HttpContext.RequestAborted);
        }
    }
}
