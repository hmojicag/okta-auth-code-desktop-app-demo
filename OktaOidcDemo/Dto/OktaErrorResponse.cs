using System.Text.Json.Serialization;

namespace OktaOidcDemo.Dto;

public class OktaErrorResponse {
    [JsonPropertyName("error")]
    public string Error { get; set; }

    [JsonPropertyName("error_description")]
    public string ErrorDescription { get; set; }
}
