namespace BackToSchool.Application.Dtos.Sales;

// ---------- Pantalla del promotor: todo lo necesario para armar un pedido, en una sola llamada ----------

public class OrderFormDto
{
    public List<SchoolDto> Schools { get; set; } = [];
    public List<OrderFormCourseDto> Courses { get; set; } = [];
    /// <summary>Todos los textos activos: para "agregar un texto que no está en la lista del curso".</summary>
    public List<OrderFormProductDto> Texts { get; set; } = [];
    public List<OrderFormProductDto> Notebooks { get; set; } = [];
    public List<OrderFormGarmentDto> Garments { get; set; } = [];
    public List<OrderFormProductDto> Others { get; set; } = [];
}

public class OrderFormCourseDto
{
    public long CourseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public List<OrderFormCourseItemDto> Items { get; set; } = [];
}

public class OrderFormCourseItemDto : OrderFormProductDto
{
    public bool IsDefaultSelected { get; set; }
}

public class OrderFormProductDto
{
    public long ProductId { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Stock { get; set; }
}

public class OrderFormGarmentDto
{
    public string GarmentName { get; set; } = string.Empty;
    /// <summary>UNIFORME o UNIFORME ALTERNATIVO (tallas de adulto).</summary>
    public string Group { get; set; } = string.Empty;
    public List<OrderFormSizeDto> Sizes { get; set; } = [];
}

public class OrderFormSizeDto
{
    public long ProductId { get; set; }
    public string? Code { get; set; }
    public string Size { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Stock { get; set; }
}

// ---------- Pedido ----------

public class SaleOrderCreateDto
{
    public string CustomerName { get; set; } = string.Empty;
    public string? TaxId { get; set; }
    public string? Phone { get; set; }
    public long? SchoolId { get; set; }
    public string? Notes { get; set; }
    public List<SaleOrderLineCreateDto> Lines { get; set; } = [];
}

public class SaleOrderLineCreateDto
{
    /// <summary>Producto del catálogo. Nulo si el promotor escribió la línea a mano.</summary>
    public long? ProductId { get; set; }
    /// <summary>Solo líneas manuales: nombre del texto.</summary>
    public string? Description { get; set; }
    /// <summary>Solo líneas manuales: precio que dicta el promotor. En el catálogo manda el precio del sistema.</summary>
    public decimal? UnitPrice { get; set; }
    public int Quantity { get; set; } = 1;
    public long? CourseId { get; set; }
    public string? StudentLabel { get; set; }
}

public class SaleOrderDto
{
    public long SaleOrderId { get; set; }
    public int TicketNumber { get; set; }
    public DateOnly TicketDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? TaxId { get; set; }
    public string? Phone { get; set; }
    public long? SchoolId { get; set; }
    public string? SchoolName { get; set; }
    public long PromoterAppUserId { get; set; }
    public string? PromoterName { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public int ItemCount { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PrintedAt { get; set; }
    public DateTime? ReadyAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
    public List<SaleOrderLineDto>? Lines { get; set; }
}

public class SaleOrderLineDto
{
    public long SaleOrderLineId { get; set; }
    public long? ProductId { get; set; }
    public string? Code { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Size { get; set; }
    public string Section { get; set; } = string.Empty;
    public long? CourseId { get; set; }
    public string? CourseName { get; set; }
    public string? StudentLabel { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
    public bool IsManual { get; set; }
}

public class SaleOrderStatusDto
{
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

// ---------- Administración del catálogo ----------

public class SchoolDto
{
    public long SchoolId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CourseDto
{
    public long CourseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public int ItemCount { get; set; }
}

public class CourseProductDto
{
    public long ProductId { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsDefaultSelected { get; set; } = true;
}

public class CourseProductsUpdateDto
{
    /// <summary>La lista completa del curso, en el orden en que debe aparecer.</summary>
    public List<CourseProductItemDto> Items { get; set; } = [];
}

public class CourseProductItemDto
{
    public long ProductId { get; set; }
    public bool IsDefaultSelected { get; set; } = true;
}

public class ProductDto
{
    public long ProductId { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string? AccountGroup { get; set; }
    public string? GarmentName { get; set; }
    public string? Size { get; set; }
    public int SizeOrder { get; set; }
    public decimal Price { get; set; }
    public decimal? UnitCost { get; set; }
    public decimal Stock { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ProductPriceDto
{
    public long ProductId { get; set; }
    public decimal Price { get; set; }
}

public class SalesSummaryDto
{
    public DateOnly TicketDate { get; set; }
    public int Orders { get; set; }
    public int Pending { get; set; }
    public int Printed { get; set; }
    public int Ready { get; set; }
    public int Cancelled { get; set; }
    public decimal Total { get; set; }
}
