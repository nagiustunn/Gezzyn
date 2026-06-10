using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace gezzyn.API.Filters;

public class SwaggerFileOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.RequestBody != null)
        {
            foreach (var content in operation.Responses.Select(x => x.Value.Content))
            {
                content.Clear();
                content.Add("application/json", new OpenApiMediaType());
            }
        }
    }
}