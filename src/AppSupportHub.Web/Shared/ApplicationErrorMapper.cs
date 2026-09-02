using AppSupportHub.Application.Abstractions.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AppSupportHub.Web.Http;

public static class ApplicationErrorMapper
{
    public static IResult ToProblem(ApplicationError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        int statusCode = GetStatusCode(error.Type);

        return Results.Problem(
            statusCode: statusCode,
            title: GetTitle(error.Type),
            detail: error.Description,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = error.Code,
            });
    }

    public static IActionResult ToPageResult(
        PageModel pageModel,
        ApplicationError error,
        string? fieldName = null)
    {
        ArgumentNullException.ThrowIfNull(pageModel);
        ArgumentNullException.ThrowIfNull(error);

        if (error.Type == ApplicationErrorType.NotFound)
        {
            return pageModel.NotFound();
        }

        pageModel.ModelState.AddModelError(fieldName ?? string.Empty, error.Description);
        return pageModel.Page();
    }

    private static int GetStatusCode(ApplicationErrorType errorType)
    {
        return errorType switch
        {
            ApplicationErrorType.Validation => StatusCodes.Status400BadRequest,
            ApplicationErrorType.NotFound => StatusCodes.Status404NotFound,
            ApplicationErrorType.Conflict => StatusCodes.Status409Conflict,
            ApplicationErrorType.BusinessRule => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError,
        };
    }

    private static string GetTitle(ApplicationErrorType errorType)
    {
        return errorType switch
        {
            ApplicationErrorType.Validation => "Invalid input",
            ApplicationErrorType.NotFound => "Resource not found",
            ApplicationErrorType.Conflict => "Conflict",
            ApplicationErrorType.BusinessRule => "Business rule conflict",
            _ => "Request failed",
        };
    }
}
