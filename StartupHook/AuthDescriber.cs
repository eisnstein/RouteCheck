using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace RouteCheck.StartupHook;

/// <summary>
/// Describes who may request an endpoint, the way the authorization middleware and
/// MVC's AuthorizeFilter decide it: "Anonymous", or what is required, e.g. "Policy: Admin".
/// </summary>
internal sealed class AuthDescriber(IServiceProvider services)
{
    private const string Anonymous = EndpointsContract.Anonymous;

    // Without AddAuthorization() there is no provider, and so no fallback policy
    private readonly IAuthorizationPolicyProvider? _policyProvider = services.GetService<IAuthorizationPolicyProvider>();

    public async Task<string> DescribeAsync(Endpoint endpoint)
    {
        var metadata = endpoint.Metadata;

        // Skips both the middleware and MVC's AuthorizeFilter
        if (metadata.GetMetadata<IAllowAnonymous>() is not null || metadata.GetMetadata<IAllowAnonymousFilter>() is not null)
        {
            return Anonymous;
        }

        var requirements = new List<string>();

        // What the authorization middleware checks
        foreach (var authorizeData in metadata.GetOrderedMetadata<IAuthorizeData>())
        {
            requirements.AddRange(await DescribeAsync(authorizeData));
        }

        foreach (var policy in metadata.GetOrderedMetadata<AuthorizationPolicy>())
        {
            requirements.AddRange(Describe(policy.Requirements));
        }

        foreach (var requirementData in metadata.GetOrderedMetadata<IAuthorizationRequirementData>())
        {
            requirements.AddRange(Describe(requirementData.GetRequirements()));
        }

        // Without anything to check, the middleware uses the fallback policy
        if (requirements.Count == 0 && await GetFallbackPolicyAsync() is { } fallbackPolicy)
        {
            requirements.AddRange(Describe(fallbackPolicy.Requirements).Select(r => $"{r} (fallback)"));
        }

        // MVC filters, e.g. a global AuthorizeFilter in MvcOptions.Filters, are endpoint metadata too
        foreach (var filter in metadata.OfType<AuthorizeFilter>())
        {
            if (filter.Policy is not null)
            {
                requirements.AddRange(Describe(filter.Policy.Requirements));
            }

            foreach (var authorizeData in filter.AuthorizeData ?? [])
            {
                requirements.AddRange(await DescribeAsync(authorizeData));
            }
        }

        return Combine(requirements);
    }

    // Roles or a policy say more than "Authenticated", which they usually include anyway
    private static string Combine(List<string> requirements)
    {
        var distinct = requirements.Distinct().ToList();
        if (distinct.Count == 0)
        {
            return Anonymous;
        }

        var specific = distinct.Where(r => !r.StartsWith("Authenticated", StringComparison.Ordinal)).ToList();
        return string.Join("; ", specific.Count > 0 ? specific : distinct);
    }

    private async Task<IEnumerable<string>> DescribeAsync(IAuthorizeData authorizeData)
    {
        var requirements = new List<string>();

        if (!string.IsNullOrWhiteSpace(authorizeData.Policy))
        {
            requirements.Add($"Policy: {authorizeData.Policy}");
        }

        if (!string.IsNullOrWhiteSpace(authorizeData.Roles))
        {
            requirements.Add($"Roles: {string.Join(", ", authorizeData.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))}");
        }

        // A plain [Authorize] uses the default policy
        if (requirements.Count == 0)
        {
            var defaultPolicy = await GetDefaultPolicyAsync();
            requirements.AddRange(defaultPolicy is null ? ["Authenticated"] : Describe(defaultPolicy.Requirements));
        }

        return requirements;
    }

    private static IEnumerable<string> Describe(IEnumerable<IAuthorizationRequirement> requirements)
    {
        return requirements.Select(requirement => requirement switch
        {
            DenyAnonymousAuthorizationRequirement => "Authenticated",
            RolesAuthorizationRequirement roles => $"Roles: {string.Join(", ", roles.AllowedRoles)}",
            ClaimsAuthorizationRequirement { AllowedValues: { } values } claim when values.Any() => $"Claim: {claim.ClaimType}={string.Join("|", values)}",
            ClaimsAuthorizationRequirement claim => $"Claim: {claim.ClaimType}",
            NameAuthorizationRequirement name => $"User: {name.RequiredName}",
            AssertionRequirement => "Assertion",
            _ => TrimSuffix(requirement.GetType().Name, "Requirement"),
        });
    }

    private static string TrimSuffix(string name, string suffix)
    {
        return name.Length > suffix.Length && name.EndsWith(suffix, StringComparison.Ordinal) ? name[..^suffix.Length] : name;
    }

    // Custom providers could fail, e.g. without a request. The routes matter more.
    private async Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
    {
        try
        {
            return _policyProvider is null ? null : await _policyProvider.GetFallbackPolicyAsync();
        }
        catch (Exception)
        {
            return null;
        }
    }

    private async Task<AuthorizationPolicy?> GetDefaultPolicyAsync()
    {
        try
        {
            return _policyProvider is null ? null : await _policyProvider.GetDefaultPolicyAsync();
        }
        catch (Exception)
        {
            return null;
        }
    }
}
