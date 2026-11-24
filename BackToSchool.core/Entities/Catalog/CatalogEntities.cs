namespace BackToSchool.core.Entities.Catalog;

/// <summary>Unidad educativa (colegio) de la que viene el papá. Solo informativo en el pedido.</summary>
public class School
{
    public long SchoolId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Curso (Pre inicial, 1ro Primaria, 6to Secundaria...). Cada curso tiene su lista de textos.</summary>
public class Course
{
    public long CourseId { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>INICIAL, PRIMARIA o SECUNDARIA.</summary>
    public string Level { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Sección de la pantalla de pedido donde aparece el producto.</summary>
public static class ProductSections
{
    public const string Text = "TEXT";
    public const string Notebook = "NOTEBOOK";
    public const string Uniform = "UNIFORM";
    public const string Other = "OTHER";

    public static readonly string[] All = [Text, Notebook, Uniform, Other];
}

public class Product
{
    public long ProductId { get; set; }
    /// <summary>Código del inventario. Opcional: hay textos que todavía no están en el inventario.</summary>
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>Ver <see cref="ProductSections"/>.</summary>
    public string Section { get; set; } = ProductSections.Other;
    /// <summary>Grupo contable del inventario (ej. "19 - UNIFORMES"). Solo referencia.</summary>
    public string? AccountGroup { get; set; }
    /// <summary>Prenda a la que pertenece la talla (ej. "BLUSA"). Solo uniformes.</summary>
    public string? GarmentName { get; set; }
    public string? Size { get; set; }
    public int SizeOrder { get; set; }
    public decimal Price { get; set; }
    public decimal? UnitCost { get; set; }
    public decimal Stock { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Texto que aparece en la lista de un curso.</summary>
public class CourseProduct
{
    public long CourseProductId { get; set; }
    public long CourseId { get; set; }
    public long ProductId { get; set; }
    public int DisplayOrder { get; set; }
    /// <summary>Aparece marcado al elegir el curso (la agenda, por ejemplo, va desmarcada).</summary>
    public bool IsDefaultSelected { get; set; } = true;
}
