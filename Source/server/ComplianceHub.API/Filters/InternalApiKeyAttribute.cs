using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ComplianceHub.API.Filters;

[AttributeUsage(AttributeTargets.Method)]
public class InternalApiKeyAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var expectedKey = config["InternalApi:ApiKey"];

        if (string.IsNullOrEmpty(expectedKey) ||
            !context.HttpContext.Request.Headers.TryGetValue("X-Internal-Api-Key", out var key) ||
            key != expectedKey)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        await next();
    }
}
