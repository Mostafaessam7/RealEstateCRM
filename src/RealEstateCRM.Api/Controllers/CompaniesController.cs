using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateCRM.Application.Companies;
using RealEstateCRM.Application.Companies.DTOs;

namespace RealEstateCRM.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/companies")]
public class CompaniesController : ControllerBase
{
    private readonly ICompanyService _companyService;

    public CompaniesController(ICompanyService companyService)
    {
        _companyService = companyService;
    }

    [HttpGet("current")]
    public async Task<ActionResult<CompanyDto>> GetCurrent(CancellationToken cancellationToken)
    {
        return Ok(await _companyService.GetCurrentAsync(cancellationToken));
    }
}
