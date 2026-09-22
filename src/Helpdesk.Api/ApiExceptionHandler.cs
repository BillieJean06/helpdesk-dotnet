using Helpdesk.Domain.Suporte;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api;

/// <summary>
/// Traduz exceções de domínio/infraestrutura para respostas HTTP na borda da Api.
/// O tratamento fica aqui, não nos controllers: é a camada certa, porque é onde a
/// exceção vira um contrato HTTP, e evita repetir try/catch em cada action.
/// </summary>
internal sealed class ApiExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        var (status, title, detail) = exception switch
        {
            DomainException e => (StatusCodes.Status400BadRequest, "Regra de negócio violada", e.Message),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict,
                "O ticket foi alterado por outra pessoa; recarregue e tente novamente", null),
            _ => (0, string.Empty, (string?)null)
        };
        if (status == 0) return false;

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails { Status = status, Title = title, Detail = detail }, ct);
        return true;
    }
}
