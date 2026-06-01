using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceCompany.Application.Features.Dashboard.Queries;

namespace ServiceCompany.Api.Controllers;

[Authorize(Policy = "CanViewReports")]
[ApiController]
[Route("api/v1/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsDto>> GetStats()
    {
        return await _mediator.Send(new GetDashboardStatsQuery());
    }
}
