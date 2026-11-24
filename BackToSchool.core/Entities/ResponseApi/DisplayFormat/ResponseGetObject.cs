using System.Net;
using System.Text.Json.Serialization;
using BackToSchool.core.Entities.ResponseApi.Details;

namespace BackToSchool.core.Entities.ResponseApi.DisplayFormat;

public class ResponseGetObject
{
    public object Data { get; set; } = new();
    public Message[] Messages { get; set; } = [];
    [JsonIgnore]
    public HttpStatusCode StatusCode { get; set; }
}