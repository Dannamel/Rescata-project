namespace Pickups.Api.Core;

/// <summary>Clase de error; decide con qué código HTTP responde la Api.</summary>
public enum ErrorType
{
    Validation,

    NotFound,

    Conflict,

    Failure
}
