using Microsoft.AspNetCore.Authorization;

namespace CashManagement.Entries.Api.Auth;

/// <summary>Requisito de autorização por <c>scope</c> OAuth (§8.1). O scope pode
/// vir em uma claim <c>scope</c> única (separada por espaço) ou em múltiplas claims.</summary>
public sealed class ScopeRequirement : IAuthorizationRequirement
{
    public ScopeRequirement(string scope) => Scope = scope;

    public string Scope { get; }
}

/// <summary>Verifica se o token apresenta o scope exigido — token sem o scope vira 403.</summary>
public sealed class ScopeHandler : AuthorizationHandler<ScopeRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ScopeRequirement requirement)
    {
        var scopes = context.User.FindAll("scope")
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        if (scopes.Contains(requirement.Scope))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
