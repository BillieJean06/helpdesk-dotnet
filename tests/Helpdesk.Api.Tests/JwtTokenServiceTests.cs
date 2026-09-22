using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Helpdesk.Api.Auth;
using Helpdesk.Api.Tenancy;
using Helpdesk.Application.Abstractions;
using Helpdesk.Infrastructure.Identity;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.Tokens;

namespace Helpdesk.Api.Tests;

public class JwtTokenServiceTests
{
    private static readonly JwtOptions Options = new()
    {
        Issuer = "helpdesk-dotnet-testes",
        Audience = "helpdesk-dotnet-testes-clientes",
        Key = "chave-de-teste-com-pelo-menos-32-caracteres",
        ExpiracaoMinutos = 60
    };

    private readonly FakeTimeProvider _relogio = new(new DateTimeOffset(2026, 1, 5, 9, 0, 0, TimeSpan.Zero));

    private static ApplicationUser NovoUsuario() => new()
    {
        Id = Guid.NewGuid(),
        Email = "atendente@helpdesk.local",
        Nome = "Atendente Demo",
        TenantId = Guid.NewGuid()
    };

    /// <summary>
    /// Reproduz a configuração de validação do Program.cs, para o teste refletir o comportamento
    /// real: (1) MapInboundClaims = false — sem isso, o handler remapeia "sub"/"email" para os
    /// tipos legados do .NET (ClaimTypes.NameIdentifier etc.) por padrão. (2) A validação padrão
    /// de expiração usa o relógio real do sistema, não um TimeProvider injetável; para testar
    /// expiração de forma determinística, substituímos por um LifetimeValidator que usa o
    /// FakeTimeProvider — só nos testes, Program.cs usa o relógio real, que é o certo em produção.
    /// </summary>
    private ClaimsPrincipal Validar(string token, bool checarExpiracaoPeloRelogioFake = false)
    {
        var handler = new JwtSecurityTokenHandler();
        handler.InboundClaimTypeMap.Clear();

        var parametros = new TokenValidationParameters
        {
            ValidIssuer = Options.Issuer,
            ValidAudience = Options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Options.Key)),
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = JwtTokenService.RoleClaimType,
            ValidateLifetime = checarExpiracaoPeloRelogioFake,
            LifetimeValidator = checarExpiracaoPeloRelogioFake
                ? (notBefore, expires, _, _) =>
                    notBefore <= _relogio.GetUtcNow().UtcDateTime && _relogio.GetUtcNow().UtcDateTime < expires
                : null
        };
        return handler.ValidateToken(token, parametros, out _);
    }

    [Fact]
    public void Gera_um_token_valido_para_o_issuer_audience_e_chave_configurados()
    {
        var usuario = NovoUsuario();
        var (token, _) = new JwtTokenService(Options, _relogio).Gerar(usuario, [Papeis.Atendente]);

        var principal = Validar(token);

        Assert.Equal(usuario.Id.ToString(), principal.FindFirstValue(JwtRegisteredClaimNames.Sub));
    }

    [Fact]
    public void Inclui_tenant_id_email_e_papeis_como_claims()
    {
        var usuario = NovoUsuario();
        var (token, _) = new JwtTokenService(Options, _relogio).Gerar(usuario, [Papeis.Atendente, Papeis.Supervisor]);

        var principal = Validar(token);

        Assert.Equal(usuario.TenantId.ToString(), principal.FindFirstValue(HttpTenantContext.ClaimType));
        Assert.Equal(usuario.Email, principal.FindFirstValue(JwtRegisteredClaimNames.Email));
        // Claim curta "role" no token (não ClaimTypes.Role, a URI longa do .NET): é o que o
        // front-end (fora do mundo .NET) precisa conseguir ler sem conhecer convenções do .NET.
        Assert.Equal([Papeis.Atendente, Papeis.Supervisor],
            principal.FindAll(JwtTokenService.RoleClaimType).Select(c => c.Value));
        Assert.True(principal.IsInRole(Papeis.Atendente));
        Assert.True(principal.IsInRole(Papeis.Supervisor));
        Assert.False(principal.IsInRole(Papeis.Cliente));
    }

    [Fact]
    public void Calcula_a_expiracao_a_partir_dos_minutos_configurados()
    {
        var (_, expiraEm) = new JwtTokenService(Options, _relogio).Gerar(NovoUsuario(), [Papeis.Cliente]);

        Assert.Equal(_relogio.GetUtcNow().AddMinutes(Options.ExpiracaoMinutos), expiraEm);
    }

    [Fact]
    public void Token_e_valido_antes_de_expirar_e_invalido_depois()
    {
        var (token, _) = new JwtTokenService(Options, _relogio).Gerar(NovoUsuario(), [Papeis.Cliente]);

        _relogio.Advance(TimeSpan.FromMinutes(Options.ExpiracaoMinutos - 1));
        Validar(token, checarExpiracaoPeloRelogioFake: true); // não lança

        _relogio.Advance(TimeSpan.FromMinutes(2));
        Assert.Throws<SecurityTokenInvalidLifetimeException>(
            () => Validar(token, checarExpiracaoPeloRelogioFake: true));
    }

    [Fact]
    public void Rejeita_token_assinado_com_chave_diferente()
    {
        var outrasOpcoes = new JwtOptions
        {
            Issuer = Options.Issuer,
            Audience = Options.Audience,
            Key = "outra-chave-completamente-diferente-32+",
            ExpiracaoMinutos = 60
        };
        var (token, _) = new JwtTokenService(outrasOpcoes, _relogio).Gerar(NovoUsuario(), [Papeis.Cliente]);

        Assert.Throws<SecurityTokenSignatureKeyNotFoundException>(() => Validar(token));
    }
}
