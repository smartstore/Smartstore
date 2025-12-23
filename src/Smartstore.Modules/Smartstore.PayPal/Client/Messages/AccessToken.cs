namespace Smartstore.PayPal.Client.Messages;

public class AccessToken
{
    private readonly DateTime _createDate;

    public AccessToken()
    {
        _createDate = DateTime.Now;
    }

    [JsonPropertyName("access_token"), JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string Token;

    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string TokenType;

    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public int ExpiresIn;

    public bool IsExpired()
    {
        var expireDate = _createDate.Add(TimeSpan.FromSeconds(ExpiresIn));
        return DateTime.Now.CompareTo(expireDate) > 0;
    }
}
