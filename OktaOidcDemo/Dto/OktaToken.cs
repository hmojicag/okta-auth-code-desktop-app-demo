using System.Text.Json.Serialization;

namespace OktaOidcDemo.Dto;

public class OktaToken {
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }

    [JsonPropertyName("scope")]
    public string Scope { get; set; }
}
