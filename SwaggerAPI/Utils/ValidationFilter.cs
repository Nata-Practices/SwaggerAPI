using SwaggerAPI.Models;

namespace SwaggerAPI.Utils;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class ValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(ms => ms.Value.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            var response = new ApiResponse<Dictionary<string, string[]>>
            {
                Success = false,
                Data = errors,
                Message = "Некорректная модель данных"
            };

            context.Result = new BadRequestObjectResult(response);
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}