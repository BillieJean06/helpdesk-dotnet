using Helpdesk.Application.Abstractions;
using Helpdesk.Domain.Suporte;
using Helpdesk.Infrastructure;
using Helpdesk.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Helpdesk.Infrastructure.Tests;

/// <summary>
/// Cria um banco descartável por execução, aplica as migrations reais e o remove no final.
/// Precisa de HELPDESK_TEST_CONNECTION apontando para um servidor onde o usuário possa CREATE DATABASE.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    public const string EnvVar = "HELPDESK_TEST_CONNECTION";

    public static string? AdminConnection => Environment.GetEnvironmentVariable(EnvVar);

    private string? _dbName;

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        if (AdminConnection is null) return;

        _dbName = $"helpdesk_test_{Guid.NewGuid():N}";

        await using (var admin = new NpgsqlConnection(AdminConnection))
        {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{_dbName}\"", admin);
            await create.ExecuteNonQueryAsync();
        }

        ConnectionString = new NpgsqlConnectionStringBuilder(AdminConnection) { Database = _dbName }.ConnectionString;

        using var sessao = AbrirSessao(Guid.NewGuid());
        await sessao.Db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_dbName is null) return;

        NpgsqlConnection.ClearAllPools();
        await using var admin = new NpgsqlConnection(AdminConnection);
        await admin.OpenAsync();
        await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_dbName}\" WITH (FORCE)", admin);
        await drop.ExecuteNonQueryAsync();
    }

    /// <summary>Uma "requisição" isolada: escopo de DI próprio, atuando em nome de um tenant.</summary>
    public Sessao AbrirSessao(Guid tenantId) => new(ConnectionString, tenantId);
}

public sealed class Sessao : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;

    internal Sessao(string connectionString, Guid tenantId)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITenantContext>(new TenantFixo(tenantId));
        services.AddInfrastructure(connectionString);
        _provider = services.BuildServiceProvider();
        _scope = _provider.CreateScope();
    }

    public ITicketRepository Repo => _scope.ServiceProvider.GetRequiredService<ITicketRepository>();

    public HelpdeskDbContext Db => _scope.ServiceProvider.GetRequiredService<HelpdeskDbContext>();

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }

    private sealed class TenantFixo(Guid tenantId) : ITenantContext
    {
        public Guid TenantId => tenantId;
    }
}

/// <summary>Ignora o teste (com motivo visível no relatório) quando não há Postgres configurado.</summary>
public sealed class FactPostgresAttribute : FactAttribute
{
    public FactPostgresAttribute()
    {
        if (PostgresFixture.AdminConnection is null)
            Skip = $"Defina {PostgresFixture.EnvVar} para rodar os testes de integração.";
    }
}
