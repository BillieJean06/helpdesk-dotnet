using System.Reflection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Helpdesk.Api;

/// <summary>
/// Marca como "required" toda propriedade cujo tipo C# não é anulável (nem Nullable&lt;T&gt;,
/// nem referência com `?`). O suporte embutido do Swashbuckle para nullable reference types
/// (<c>SupportNonNullableReferenceTypes</c>) não cobre isso de forma confiável para records e
/// tipos por valor; <see cref="NullabilityInfoContext"/> lê a anotação real do compilador.
/// Sem isso, os DTOs geram TypeScript com todo campo opcional no front, obrigando a usar `!`
/// (non-null assertion) em vez do tipo real.
/// </summary>
internal sealed class RequiredPropertiesSchemaFilter : ISchemaFilter
{
    private static readonly NullabilityInfoContext NullabilityContext = new();

    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema.Properties is null || schema.Properties.Count == 0) return;

        var propriedades = context.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var nomeJson in schema.Properties.Keys)
        {
            var propriedade = propriedades
                .FirstOrDefault(p => string.Equals(p.Name, nomeJson, StringComparison.OrdinalIgnoreCase));
            if (propriedade is null) continue;

            var anulavel = Nullable.GetUnderlyingType(propriedade.PropertyType) is not null
                || NullabilityContext.Create(propriedade).WriteState == NullabilityState.Nullable;

            if (!anulavel) schema.Required.Add(nomeJson);
        }
    }
}
