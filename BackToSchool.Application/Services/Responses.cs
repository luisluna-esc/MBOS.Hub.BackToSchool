using System.Net;
using BackToSchool.core.Entities.ResponseApi.Details;
using BackToSchool.core.Entities.ResponseApi.DisplayFormat;

namespace BackToSchool.Application.Services;

/// <summary>Atajos para armar las respuestas estándar de la API (mismo formato que el resto de los servicios).</summary>
internal static class Responses
{
    public static ResponseGetObject Ok(object data, HttpStatusCode status = HttpStatusCode.OK) =>
        new() { Data = data, Messages = [], StatusCode = status };

    public static ResponseGetObject GetError(HttpStatusCode status, params string[] errors) =>
        new() { Data = new(), Messages = errors.Select(Error).ToArray(), StatusCode = status };

    public static ResponsePost Saved(long id, string message, HttpStatusCode status = HttpStatusCode.OK) =>
        new() { Id = id, Messages = [new Message { Type = MessageType.Success, Description = message }], StatusCode = status };

    public static ResponsePost PostError(long id, HttpStatusCode status, params string[] errors) =>
        new() { Id = id, Messages = errors.Select(Error).ToArray(), StatusCode = status };

    private static Message Error(string description) => new() { Type = MessageType.Error, Description = description };
}
