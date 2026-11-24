namespace BackToSchool.core.Entities.Sales;

public static class SaleOrderStatuses
{
    /// <summary>Recién enviado por el promotor; almacén todavía no lo imprimió.</summary>
    public const string Pending = "PENDING";
    /// <summary>Almacén lo imprimió y está sacando los materiales.</summary>
    public const string Printed = "PRINTED";
    /// <summary>Los materiales ya están en caja esperando al papá.</summary>
    public const string Ready = "READY";
    public const string Cancelled = "CANCELLED";

    public static readonly string[] All = [Pending, Printed, Ready, Cancelled];
}

public class SaleOrder
{
    public long SaleOrderId { get; set; }
    /// <summary>Número de ficha con el que caja llama al papá. Vuelve a 1 cada día.</summary>
    public int TicketNumber { get; set; }
    public DateOnly TicketDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? TaxId { get; set; }
    public string? Phone { get; set; }
    public long? SchoolId { get; set; }
    public string? SchoolName { get; set; }
    public long PromoterAppUserId { get; set; }
    public string Status { get; set; } = SaleOrderStatuses.Pending;
    public decimal Total { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PrintedAt { get; set; }
    public DateTime? ReadyAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }

    public List<SaleOrderLine> Lines { get; set; } = [];
}

public class SaleOrderLine
{
    public long SaleOrderLineId { get; set; }
    public long SaleOrderId { get; set; }
    /// <summary>Nulo en las líneas escritas a mano por el promotor.</summary>
    public long? ProductId { get; set; }
    public string? Code { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Size { get; set; }
    public string Section { get; set; } = string.Empty;
    public long? CourseId { get; set; }
    public string? CourseName { get; set; }
    /// <summary>Hijo al que corresponde (ej. "Hijo 1"), para agrupar en la hoja de almacén.</summary>
    public string? StudentLabel { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
    public bool IsManual { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>Último número de ficha entregado en el día. Se incrementa de forma atómica en la base.</summary>
public class TicketCounter
{
    public DateOnly TicketDate { get; set; }
    public int LastNumber { get; set; }
}
