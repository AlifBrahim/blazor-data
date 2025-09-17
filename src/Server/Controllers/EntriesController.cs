using Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Services;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EntriesController : ControllerBase
{
    private readonly ProductEntryService _service;

    public EntriesController(ProductEntryService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ProductEntry entry, CancellationToken cancellationToken)
    {
        var existing = await _service.FindAsync(entry.Id, cancellationToken);
        if (existing is not null)
        {
            return Conflict();
        }

        var result = await _service.SaveAsync(entry, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductEntry>>> Get(CancellationToken cancellationToken)
    {
        var entries = await _service.GetLatestAsync(200, cancellationToken);
        return Ok(entries);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductEntry>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var entry = await _service.FindAsync(id, cancellationToken);
        return entry is null ? NotFound() : Ok(entry);
    }
}
