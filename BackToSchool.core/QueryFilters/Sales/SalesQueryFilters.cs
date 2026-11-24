namespace BackToSchool.core.QueryFilters.Sales;

public class ProductQueryFilter
{
    public string? Section { get; set; }
    public bool? IsActive { get; set; }
    public bool? WithoutPrice { get; set; }
}

public class SaleOrderQueryFilter
{
    /// <summary>Día de las fichas (por defecto, hoy en Bolivia).</summary>
    public DateOnly? TicketDate { get; set; }
    public string? Status { get; set; }
    public int? TicketNumber { get; set; }
    /// <summary>Solo los pedidos del promotor que consulta.</summary>
    public bool Mine { get; set; }
}
