using BackToSchool.Application.Dtos.Sales;
using BackToSchool.core.Entities.ResponseApi.DisplayFormat;
using BackToSchool.core.QueryFilters.Pagination;
using BackToSchool.core.QueryFilters.Sales;

namespace BackToSchool.Application.Interfaces.Sales;

public interface ICatalogService
{
    Task<ResponseGetObject> GetOrderForm();

    Task<ResponseGetObject> GetSchools(bool includeInactive);
    Task<ResponsePost> SaveSchool(long? id, SchoolDto schoolDto);

    Task<ResponseGetObject> GetCourses();
    Task<ResponseGetObject> GetCourseProducts(long courseId);
    Task<ResponsePost> UpdateCourseProducts(long courseId, CourseProductsUpdateDto dto);

    Task<ResponseGetObject> GetProducts(PaginationQueryFilter pagination, ProductQueryFilter filter);
    Task<ResponsePost> SaveProduct(long? id, ProductDto productDto);
    Task<ResponsePost> UpdatePrices(List<ProductPriceDto> prices);
}

public interface ISaleOrderService
{
    Task<ResponseGetObject> Create(SaleOrderCreateDto dto);
    Task<ResponseGetObject> GetAll(SaleOrderQueryFilter filter);
    Task<ResponseGetObject> GetById(long id);
    Task<ResponsePost> ChangeStatus(long id, SaleOrderStatusDto dto);
    Task<ResponseGetObject> GetSummary(DateOnly? ticketDate);
}

public interface IReportService
{
    /// <summary>Hoja del pedido para almacén. Al imprimirla, el pedido pendiente pasa a "Impreso".</summary>
    Task<(byte[]? Pdf, string FileName)> GetSaleOrderPdf(long saleOrderId);
}
