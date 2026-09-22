using Helpdesk.Api.Tenancy;
using Helpdesk.Application.Abstractions;
using Helpdesk.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// A connection string vem de user-secrets (dev) ou variável de ambiente (prod); nunca do repositório.
var connectionString = builder.Configuration.GetConnectionString("Helpdesk")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:Helpdesk não configurada. Em dev: dotnet user-secrets set \"ConnectionStrings:Helpdesk\" \"<conexão>\" --project src/Helpdesk.Api");

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, HttpTenantContext>();
builder.Services.AddInfrastructure(connectionString);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
