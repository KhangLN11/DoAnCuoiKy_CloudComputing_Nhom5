
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace StudentFeedback.Swagger;

public sealed class SwaggerAntiforgeryOperationFilter
    : IOperationFilter
{
    public void Apply(
        OpenApiOperation operation,
        OperationFilterContext context)
    {
        string? method = context.ApiDescription.HttpMethod;

        if (string.IsNullOrWhiteSpace(method))
        {
            return;
        }

        // Chỉ các HTTP method có thể thay đổi dữ liệu.
        if (method != "POST" &&
            method != "PUT" &&
            method != "PATCH" &&
            method != "DELETE")
        {
            return;
        }

        string path =
            context.ApiDescription.RelativePath ?? "";

        // Chỉ áp dụng cho REST API.
        if (!path.StartsWith(
            "api/",
            StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        operation.Parameters ??=
            new List<OpenApiParameter>();

        // Tránh thêm header trùng lặp.
        if (operation.Parameters.Any(p =>
            p.Name == "RequestVerificationToken" &&
            p.In == ParameterLocation.Header))
        {
            return;
        }

        operation.Parameters.Add(
            new OpenApiParameter
            {
                Name = "RequestVerificationToken",

                In = ParameterLocation.Header,

                Required = true,

                Description =
                    "Lấy requestToken từ GET /api/antiforgery/token.",

                Schema = new OpenApiSchema
                {
                    Type = "string"
                }
            });
    }
}
