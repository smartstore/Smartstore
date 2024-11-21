namespace Smartstore.PayPal.Client.Messages
{
    public class ExceptionMessage
    {
        public string Name;
        public string Message;
        public string DebugId;
        public List<ExceptionDetails> Details;
        public List<LinkDescription> Links;
    }

    public class ExceptionDetails
    {
        public string Field;
        public string Issue;
        public string Description;
    }
}
