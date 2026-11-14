using Microsoft.AspNetCore.Mvc;

namespace Pickups.Api.Core;


public static class ResultExtensions
{
    public static IActionResult ToActionResult(this Result result) =>
        result.IsSuccess
            ? new NoContentResult()
            : result.Error.ToErrorResult();

    public static IActionResult ToActionResult<TValue>(this Result<TValue> result) =>
        result.IsSuccess
            ? new OkObjectResult(result.Value)
            : result.Error.ToErrorResult();

   
    public static IActionResult ToErrorResult(this Error error)
    {
        int statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        return new ObjectResult(new
        {
            status = statusCode,
            error = error.Code,
            message = error.Description
        })
        {
            StatusCode = statusCode
        };
    }
}
