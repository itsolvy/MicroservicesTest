namespace MicroservicesTest.SenderApi.Db
{
    public class Order
    {
        public Guid Id { get; internal set; }
        public decimal Cost { get; internal set; }
        public int GoodId { get; internal set; }
        public bool MessageSend { get; internal set; }
    }

}
