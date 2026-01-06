namespace Smartstore.Clickatell.Client.Messages;

public class ClickatellResponse
{
    public List<ClickatellMessage> Messages { get; set; }
    public int ErrorCode { get; set; }
    public string Error { get; set; }
    public string ErrorDescription { get; set; }
}

public class ClickatellMessage
{
    public string ApiMessageId { get; set; }
    public bool Accepted { get; set; }
    public string To { get; set; }
    public int ErrorCode { get; set; }
    public string Error { get; set; }
    public string ErrorDescription { get; set; }
}