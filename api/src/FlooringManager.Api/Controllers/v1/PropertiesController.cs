using System.Runtime.Versioning;
using FlooringManager.Application.Properties;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlooringManager.Api.Controllers.v1;

[ApiController]
[Route("api/v1/customers/{customerId:guid}/properties")]
[Authorize]
public sealed class CustomerPropertiesController(IPropertyService properties) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PropertyResponse>>> List(Guid customerId, CancellationToken ct)
    {
        var items = await properties.ListForCustomerAsync(customerId, ct);
        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<PropertyResponse>> Create(
        Guid customerId,
        [FromBody] CreatePropertyRequest request,
        CancellationToken ct)
    {
        var created = await properties.CreateForCustomerAsync(customerId, request, ct);
        if (created is null) return NotFound();

        return CreatedAtAction(nameof(PropertiesController.Get),
            controllerName: "Properties",
            new { id = created.Id },
            created);
    }
}

[ApiController]
[Route("api/v1/properties")]
[Authorize]
public sealed class PropertiesController(IPropertyService properties) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PropertyResponse>> Get(Guid id, CancellationToken ct)
    {
        var property = await properties.GetAsync(id, ct);
        return property is null ? NotFound() : Ok(property);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PropertyResponse>> Update(Guid id, [FromBody] UpdatePropertyRequest request, CancellationToken ct)
    {
        var updated = await properties.UpdateAsync(id, request, ct);
        return updated is null ? NotFound() : Ok(updated);
    }
}