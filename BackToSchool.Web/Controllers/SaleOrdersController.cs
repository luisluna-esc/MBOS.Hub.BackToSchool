using BackToSchool.Application.Dtos.Sales;
using BackToSchool.Application.Interfaces.Sales;
using BackToSchool.core.QueryFilters.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackToSchool.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SaleOrdersController : ControllerBase
{
    private const string Warehouse = "M-BOS,Administrador,Almacén";

    private readonly ISaleOrderService _saleOrderService;
    private readonly IReportService _reportService;

    public SaleOrdersController(ISaleOrderService saleOrderService, IReportService reportService)
    {
        _saleOrderService = saleOrderService;
        _reportService = reportService;
    }

    [Authorize(Policy = "order.view")]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] SaleOrderQueryFilter filter)
    {
        var result = await _saleOrderService.GetAll(filter);
        return StatusCode((int)result.StatusCode, result);
    }

    [Authorize(Policy = "order.view")]
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] DateOnly? ticketDate)
    {
        var result = await _saleOrderService.GetSummary(ticketDate);
        return StatusCode((int)result.StatusCode, result);
    }

    [Authorize(Policy = "order.view")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _saleOrderService.GetById(id);
        return StatusCode((int)result.StatusCode, result);
    }

    [Authorize(Policy = "order.create")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaleOrderCreateDto dto)
    {
        var result = await _saleOrderService.Create(dto);
        return StatusCode((int)result.StatusCode, result);
    }

    /// <summary>Cambio de estado. El servicio decide quién puede: almacén todo; el promotor, anular lo suyo si sigue pendiente.</summary>
    [Authorize(Policy = "order.create")]
    [HttpPut("{id}/status")]
    public async Task<IActionResult> ChangeStatus(long id, [FromBody] SaleOrderStatusDto dto)
    {
        var result = await _saleOrderService.ChangeStatus(id, dto);
        return StatusCode((int)result.StatusCode, result);
    }

    [Authorize(Roles = Warehouse)]
    [HttpGet("{id}/pdf")]
    public async Task<IActionResult> GetPdf(long id)
    {
        var (pdf, fileName) = await _reportService.GetSaleOrderPdf(id);
        if (pdf is null)
        {
            return NotFound();
        }
        Response.Headers.ContentDisposition = $"inline; filename=\"{fileName}\"";
        return File(pdf, "application/pdf");
    }
}
