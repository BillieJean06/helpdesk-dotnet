using Helpdesk.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Helpdesk.Infrastructure.Persistence;

/// <summary>Usada só pelo <c>dotnet ef</c>, para não precisar subir a API inteira.</summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<HelpdeskDbContext>
{
    public HelpdeskDbContext CreateDbContext(string[] args)
    {
        // `migrations add` não conecta; `database update` lê a conexão do ambiente.
        var connection = Environment.GetEnvironmentVariable("HELPDESK_DEV_CONNECTION")
            ?? "Host=localhost;Database=helpdesk_design";

        var options = new DbContextOptionsBuilder<HelpdeskDbContext>()
            .UseNpgsql(connection)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new HelpdeskDbContext(options, new TenantDeDesignTime());
    }

    private sealed class TenantDeDesignTime : ITenantContext
    {
        // `dotnet ef` só precisa montar o modelo (para gerar/aplicar migrations);
        // o filtro global nunca é avaliado nesse fluxo.
        public Guid TenantId => Guid.Empty;
    }
}
