using System.Net;

namespace Core.Api.ExceptionHandlers;

public sealed class UnknownStatusCodeException(string endpointGroupName, string endpointName, HttpStatusCode statusCode)
    : Exception($"Endpoint group: {endpointGroupName} -> Endpoint: {endpointName} -> Unknown status code: {statusCode}");
