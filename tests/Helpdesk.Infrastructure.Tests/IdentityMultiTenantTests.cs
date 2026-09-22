using Helpdesk.Infrastructure.Identity;

namespace Helpdesk.Infrastructure.Tests;

/// <summary>
/// Prova a correção do TenantAwareUserValidator: e-mail é único por empresa, não globalmente.
/// </summary>
public class IdentityMultiTenantTests(PostgresFixture pg) : IClassFixture<PostgresFixture>
{
    private static ApplicationUser NovoUsuario(Guid tenantId, string email) => new()
    {
        UserName = email,
        Email = email,
        Nome = "Usuário de Teste",
        TenantId = tenantId
    };

    [FactPostgres]
    public async Task Duas_empresas_diferentes_podem_ter_um_usuario_com_o_mesmo_email()
    {
        var email = $"mesmo.email.{Guid.NewGuid():N}@teste.com";
        var empresaA = Guid.NewGuid();
        var empresaB = Guid.NewGuid();

        using var sessao = pg.AbrirSessao(Guid.NewGuid());

        var resultadoA = await sessao.Usuarios.CreateAsync(NovoUsuario(empresaA, email), "Senha123$");
        var resultadoB = await sessao.Usuarios.CreateAsync(NovoUsuario(empresaB, email), "Senha123$");

        Assert.True(resultadoA.Succeeded, string.Join("; ", resultadoA.Errors.Select(e => e.Description)));
        Assert.True(resultadoB.Succeeded, string.Join("; ", resultadoB.Errors.Select(e => e.Description)));
    }

    [FactPostgres]
    public async Task A_mesma_empresa_nao_pode_ter_dois_usuarios_com_o_mesmo_email()
    {
        var email = $"duplicado.{Guid.NewGuid():N}@teste.com";
        var empresa = Guid.NewGuid();

        using var sessao = pg.AbrirSessao(Guid.NewGuid());
        await sessao.Usuarios.CreateAsync(NovoUsuario(empresa, email), "Senha123$");

        var segundo = await sessao.Usuarios.CreateAsync(NovoUsuario(empresa, email), "Senha123$");

        Assert.False(segundo.Succeeded);
        Assert.Contains(segundo.Errors, e => e.Code == "DuplicateEmail");
    }

    [FactPostgres]
    public async Task Login_encontra_o_usuario_certo_quando_duas_empresas_compartilham_o_email()
    {
        var email = $"login.{Guid.NewGuid():N}@teste.com";
        var empresaA = Guid.NewGuid();
        var empresaB = Guid.NewGuid();

        using var sessao = pg.AbrirSessao(Guid.NewGuid());
        await sessao.Usuarios.CreateAsync(NovoUsuario(empresaA, email), "SenhaA123$");
        await sessao.Usuarios.CreateAsync(NovoUsuario(empresaB, email), "SenhaB123$");

        var normalizado = sessao.Usuarios.NormalizeEmail(email);
        var encontradoNaEmpresaA = sessao.Usuarios.Users
            .Single(u => u.TenantId == empresaA && u.NormalizedEmail == normalizado);

        Assert.True(await sessao.Usuarios.CheckPasswordAsync(encontradoNaEmpresaA, "SenhaA123$"));
        Assert.False(await sessao.Usuarios.CheckPasswordAsync(encontradoNaEmpresaA, "SenhaB123$"));
    }
}
