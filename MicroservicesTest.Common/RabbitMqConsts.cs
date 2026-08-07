namespace MicroservicesTest.Common
{
    public static class RabbitMqConsts
    {
        public const string ORDERS_EXCHANGE = "orders.exchnage";
        public const string ORDERS_QUEQUE = "orders.queque";

        public const string LOG_EXCHANGE = "logs.http";
        public const string LOG_QUEQUE_1 = "logs.console";
        public const string LOG_QUEQUE_2 = "logs.file";
    }
}
