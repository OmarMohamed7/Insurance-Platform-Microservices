using Claims.Application;
using Claims.Application.Handlers;
using Claims.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Claims.Api;

[ApiController]
[Route("claims")]
public class ClaimsController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> SubmitClaim([FromBody] SubmitClaimCommand command, SubmitClaimHandler handler ,CancellationToken ct)
    {
        var id = await handler.Handle(command, ct);
        return CreatedAtAction(nameof(SubmitClaim), new {id}, new {id});
    }

    [HttpGet]
    public async Task<IActionResult> ListClaims(
        ListClaimsHandler handler,
        CancellationToken ct,
        [FromQuery] ClaimStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var claims = await handler.Handle(new ListClaimsQuery(status, page, pageSize), ct);
        return Ok(claims);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetClaim([FromRoute] Guid id, GetClaimHandler handler ,CancellationToken ct)
    {
        var command = new GetClaimQueryCommand(id);
        var claim = await handler.Handle(command, ct);
        if (claim is null) return NotFound();
        return Ok(claim);
    }

    [HttpPost("{id:guid}/startReview")]
    public async Task<IActionResult> StartReview([FromRoute] Guid id, StartReviewClaimHandler handler , CancellationToken ct)
    {
        var command = new StartReviewClaimCommand(id);
        var res = await handler.Handle(command, ct);
        return Ok(res);
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> ApproveClaim([FromRoute] Guid id, ApproveClaimHandler handler,
        CancellationToken ct)
    {
        var command = new ApproveClaimCommand(id, "Admin");
        var res = await handler.Handle(command, ct);
        
        return Ok(res);
    }
    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> RejectClaims([FromRoute] Guid id, [FromBody] RejectClaimRequest request,RejectClaimHandler
            handler,
        CancellationToken ct)
    {
        var command = new RejectClaimCommand(id, request.Reason,  "Admin");
        var res = await handler.Handle(command, ct);
        
        return Ok(res);
    }
}