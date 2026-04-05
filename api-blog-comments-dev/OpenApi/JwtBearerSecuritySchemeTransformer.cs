using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace api_blog_comments_dev.OpenApi;

public sealed class JwtBearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiOperationTransformer
{
    public async Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (!authenticationSchemes.Any(scheme => scheme.Name == JwtBearerDefaults.AuthenticationScheme))
        {
            return;
        }

        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        if (metadata.OfType<IAllowAnonymous>().Any() || !metadata.OfType<IAuthorizeData>().Any())
        {
            return;
        }

        var document = context.Document;
        if (document is null)
        {
            return;
        }

        var components = document.Components ?? new OpenApiComponents();
        document.Components = components;

        components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        components.SecuritySchemes[JwtBearerDefaults.AuthenticationScheme] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            In = ParameterLocation.Header,
            BearerFormat = "JWT",
            Description = "Informe o token JWT no header Authorization usando o formato: Bearer {token}."
        };

        var bearerSecurityReference = new OpenApiSecuritySchemeReference(
            JwtBearerDefaults.AuthenticationScheme,
            document,
            string.Empty);

        operation.Security ??= new List<OpenApiSecurityRequirement>();
        if (operation.Security.Any(requirement => requirement.Keys.Any(key => key.Reference?.Id == JwtBearerDefaults.AuthenticationScheme)))
        {
            return;
        }

        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [bearerSecurityReference] = []
        });
    }
}