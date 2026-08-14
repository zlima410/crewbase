namespace FlooringManager.Domain.Shared;

/// <summary>
/// Marks an entity that stores its owning company directly. Entities that reach
/// their company through a parent (for example Property → Customer) deliberately
/// do not implement this; they get a dedicated scoping overload instead.
/// </summary>
public interface ITenantOwned
{
    Guid CompanyId { get; }
}
