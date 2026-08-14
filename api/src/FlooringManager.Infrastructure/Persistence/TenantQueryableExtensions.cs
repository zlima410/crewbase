using FlooringManager.Domain.Properties;
using FlooringManager.Domain.Shared;

namespace FlooringManager.Infrastructure.Persistence;

/// <summary>
/// Company scoping for tenant-owned queries. Every read or write path against a
/// business record starts here so the filter cannot be forgotten on a new entity:
/// if it stores <c>CompanyId</c> it is reachable only through
/// <see cref="ForCompany{T}"/>, and indirect owners get an explicit overload.
/// </summary>
public static class TenantQueryableExtensions
{
    public static IQueryable<T> ForCompany<T>(this IQueryable<T> source, Guid companyId)
        where T : class, ITenantOwned =>
        source.Where(e => e.CompanyId == companyId);

    /// <summary>
    /// Property has no CompanyId of its own — it is owned by the company that owns
    /// its customer.
    /// </summary>
    public static IQueryable<Property> ForCompany(this IQueryable<Property> source, Guid companyId) =>
        source.Where(p => p.Customer.CompanyId == companyId);
}
