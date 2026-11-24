using BackToSchool.Application.Dtos.Sales;
using BackToSchool.Application.Interfaces.Sales;
using BackToSchool.core.QueryFilters.Pagination;
using BackToSchool.core.QueryFilters.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackToSchool.Web.Controllers;

/// <summary>Catálogo: lo que ve el promotor al armar un pedido y lo que administra el encargado (precios, listas por curso, colegios).</summary>
[ApiController]
[Route("api/[controller]")]
public class CatalogController : ControllerBase
{
    private const string Managers = "M-BOS,Administrador";

    private readonly ICatalogService _catalogService;

    public CatalogController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [Authorize(Policy = "order.view")]
    [HttpGet("order-form")]
    public async Task<IActionResult> GetOrderForm()
    {
        var result = await _catalogService.GetOrderForm();
        return StatusCode((int)result.StatusCode, result);
    }

    // ----- colegios

    [Authorize(Policy = "order.view")]
    [HttpGet("schools")]
    public async Task<IActionResult> GetSchools([FromQuery] bool includeInactive = false)
    {
        var result = await _catalogService.GetSchools(includeInactive);
        return StatusCode((int)result.StatusCode, result);
    }

    [Authorize(Roles = Managers)]
    [HttpPost("schools")]
    public async Task<IActionResult> InsertSchool([FromBody] SchoolDto dto)
    {
        var result = await _catalogService.SaveSchool(null, dto);
        return StatusCode((int)result.StatusCode, result);
    }

    [Authorize(Roles = Managers)]
    [HttpPut("schools/{id}")]
    public async Task<IActionResult> UpdateSchool(long id, [FromBody] SchoolDto dto)
    {
        var result = await _catalogService.SaveSchool(id, dto);
        return StatusCode((int)result.StatusCode, result);
    }

    // ----- cursos

    [Authorize(Policy = "order.view")]
    [HttpGet("courses")]
    public async Task<IActionResult> GetCourses()
    {
        var result = await _catalogService.GetCourses();
        return StatusCode((int)result.StatusCode, result);
    }

    [Authorize(Policy = "order.view")]
    [HttpGet("courses/{id}/products")]
    public async Task<IActionResult> GetCourseProducts(long id)
    {
        var result = await _catalogService.GetCourseProducts(id);
        return StatusCode((int)result.StatusCode, result);
    }

    [Authorize(Roles = Managers)]
    [HttpPut("courses/{id}/products")]
    public async Task<IActionResult> UpdateCourseProducts(long id, [FromBody] CourseProductsUpdateDto dto)
    {
        var result = await _catalogService.UpdateCourseProducts(id, dto);
        return StatusCode((int)result.StatusCode, result);
    }

    // ----- productos

    [Authorize(Policy = "order.view")]
    [HttpGet("products")]
    public async Task<IActionResult> GetProducts([FromQuery] PaginationQueryFilter pagination, [FromQuery] ProductQueryFilter filter)
    {
        var result = await _catalogService.GetProducts(pagination, filter);
        return StatusCode((int)result.StatusCode, result);
    }

    [Authorize(Roles = Managers)]
    [HttpPost("products")]
    public async Task<IActionResult> InsertProduct([FromBody] ProductDto dto)
    {
        var result = await _catalogService.SaveProduct(null, dto);
        return StatusCode((int)result.StatusCode, result);
    }

    [Authorize(Roles = Managers)]
    [HttpPut("products/{id}")]
    public async Task<IActionResult> UpdateProduct(long id, [FromBody] ProductDto dto)
    {
        var result = await _catalogService.SaveProduct(id, dto);
        return StatusCode((int)result.StatusCode, result);
    }

    [Authorize(Roles = Managers)]
    [HttpPut("products/prices")]
    public async Task<IActionResult> UpdatePrices([FromBody] List<ProductPriceDto> prices)
    {
        var result = await _catalogService.UpdatePrices(prices);
        return StatusCode((int)result.StatusCode, result);
    }
}
