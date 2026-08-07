using MicroservicesTest.Common;
using MicroservicesTest.SenderApi.Db;

namespace MicroservicesTest.SenderApi.Modules.Orders
{
    public class OrderModule(AppDbContext db, RabbitMqService rabbitMq)
    {
      

        public Task SaveOrder(OrderDto order, CancellationToken token)
        {
            var orderDb = new Order()
            {
                Id = Guid.NewGuid(),
                Cost = order.Cost,
                GoodId = order.GoodId,
                MessageSend = false
            };
            db.Orders.Add(orderDb);
            //await rabbitMq.SendMessage(orderDb);
            return db.SaveChangesAsync(token);
        }

        public async Task RecurrentSend(CancellationToken token)
        {
            var toSend = db.Orders.Where(x => !x.MessageSend).ToArray();
            foreach (var message in toSend)
            {
                await rabbitMq.SendMessageDirect(RabbitMqConsts.ORDERS_QUEQUE, message);
                message.MessageSend = true;
                db.SaveChanges();
            }
        }
    }
}