using System.Text;
using System.Text.Json.Serialization;
using Helpdesk.Api;
using Helpdesk.Api.Auth;
using Helpdesk.Api.Tenancy;
using Helpdesk.Application.Abstractions;
using Helpdesk.Application.Suporte.UseCases;
using Helpdesk.Infrastructure;
using Helpdesk.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// A connection string vem de user-secrets (dev) ou variável de ambiente (prod); nunca do repositório.
var connectionString = builder.Configuration.GetConnectionString("Helpdesk")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:Helpdesk não configurada. Em dev: dotnet user-secrets set \"ConnectionStrings:Helpdesk\" \"<conexão>\" --project src/Helpdesk.Api");

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, HttpTenantContext>();
builder.Services.AddScoped<IUsuarioAtual, HttpUsuarioAtual>();
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddScoped<AbrirTicketUseCase>();
builder.Services.AddScoped<AssumirTicketUseCase>();
builder.Services.AddScoped<ObterTicketUseCase>();
builder.Services.AddScoped<ListarTicketsUseCase>();
builder.Services.AddScoped<ComentarTicketUseCase>();

// Jwt:Key é secreto (assinatura do token) e vem de user-secrets/variável de ambiente;
// Issuer e Audience não são segredo e ficam em appsettings.json.
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtKey = jwtSection["Key"]
    ?? throw new InvalidOperationException(
        "Jwt:Key não configurada. Em dev: dotnet user-secrets set \"Jwt:Key\" \"<chave de ao menos 32 caracteres>\" --project src/Helpdesk.Api");
var jwtOptions = new JwtOptions
{
    Issuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer não configurada."),
    Audience = jwtSection["Audience"] ?? throw new InvalidOperationException("Jwt:Audience não configurada."),
    Key = jwtKey,
    ExpiracaoMinutos = jwtSection.GetValue<int?>("ExpiracaoMinutos") ?? 60
};
builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton<JwtTokenService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Mantém os nomes de claim exatamente como emitidos (ex.: "sub", "tenant_id"),
        // sem o remapeamento legado que o handler faz por padrão.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromSeconds(30),
            // Sem isso, [Authorize(Roles=...)] e User.IsInRole procurariam a claim de papel
            // pela URI longa padrão do .NET (ClaimTypes.Role), não pela claim curta "role"
            // que o JwtTokenService realmente emite.
            RoleClaimType = JwtTokenService.RoleClaimType
        };
    });
builder.Services.AddAuthorization();

// Origens do front-end (não é segredo, mas fica configurável fora do código para variar por
// ambiente). Sem "AllowAnyOrigin": a lista é explícita mesmo em dev.
const string CorsPolicyFrontend = "Frontend";
var origensPermitidas = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy(CorsPolicyFrontend, policy =>
    policy.WithOrigins(origensPermitidas).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

// Add services to the container.

builder.Services.AddControllers()
    // Enums como texto no JSON ("Alta", não 3): mais legível na API e consistente
    // com o mapeamento do EF Core, que já grava Prioridade e Status como texto no banco.
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SupportNonNullableReferenceTypes();
    options.SchemaFilter<RequiredPropertiesSchemaFilter>();
});

var app = builder.Build();

// Só em dev: cria os papéis e um usuário de exemplo por papel, se ainda não existirem.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await IdentitySeeder.SeedAsync(scope.ServiceProvider);
}

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(CorsPolicyFrontend);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Necessário para o WebApplicationFactory em testes de integração enxergar esta classe.
public partial class Program;
