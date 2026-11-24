using System.Net;
using BackToSchool.Application.Dtos.Sales;
using BackToSchool.Application.Interfaces.Sales;
using BackToSchool.core.Configuration;
using BackToSchool.core.Entities.Catalog;
using BackToSchool.core.Entities.ResponseApi.DisplayFormat;
using BackToSchool.core.Interfaces;
using BackToSchool.core.QueryFilters.Pagination;
using BackToSchool.core.QueryFilters.Sales;
using Microsoft.EntityFrameworkCore;

namespace BackToSchool.Application.Services;

public class CatalogService : ICatalogService
{
    private const string AlternativeUniformGroup = "UNIFORME ALTERNATIVO";

    private readonly IUnitOfWork _unitOfWork;
    private readonly PaginationOptions _paginationOptions;

    public CatalogService(IUnitOfWork unitOfWork, PaginationOptions paginationOptions)
    {
        _unitOfWork = unitOfWork;
        _paginationOptions = paginationOptions;
    }

    // ------------------------------------------------------------------ pantalla del promotor

    public async Task<ResponseGetObject> GetOrderForm()
    {
        var products = await _unitOfWork.Repository<Product>().Query()
            .Where(p => p.IsActive)
            .ToListAsync();
        var productsById = products.ToDictionary(p => p.ProductId);

        var courses = await _unitOfWork.Repository<Course>().Query()
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();

        var courseProducts = await _unitOfWork.Repository<CourseProduct>().Query()
            .OrderBy(cp => cp.DisplayOrder)
            .ToListAsync();

        var schools = await _unitOfWork.Repository<School>().Query()
            .Where(s => s.IsActive)
            .OrderBy(s => s.DisplayOrder).ThenBy(s => s.Name)
            .ToListAsync();

        var form = new OrderFormDto
        {
            Schools = schools.Select(ToDto).ToList(),
            Courses = courses.Select(course => new OrderFormCourseDto
            {
                CourseId = course.CourseId,
                Name = course.Name,
                Level = course.Level,
                Items = courseProducts
                    .Where(cp => cp.CourseId == course.CourseId && productsById.ContainsKey(cp.ProductId))
                    .Select(cp =>
                    {
                        var product = productsById[cp.ProductId];
                        return new OrderFormCourseItemDto
                        {
                            ProductId = product.ProductId,
                            Code = product.Code,
                            Name = product.Name,
                            Price = product.Price,
                            Stock = product.Stock,
                            IsDefaultSelected = cp.IsDefaultSelected
                        };
                    })
                    .ToList()
            }).ToList(),
            Texts = ToFormProducts(products, ProductSections.Text),
            Notebooks = ToFormProducts(products, ProductSections.Notebook),
            Others = ToFormProducts(products, ProductSections.Other),
            Garments = products
                .Where(p => p.Section == ProductSections.Uniform)
                .GroupBy(p => new
                {
                    Garment = p.GarmentName ?? p.Name,
                    Alternative = (p.AccountGroup ?? "").Contains(AlternativeUniformGroup)
                })
                .OrderBy(g => g.Key.Alternative).ThenBy(g => g.Key.Garment)
                .Select(g => new OrderFormGarmentDto
                {
                    GarmentName = g.Key.Garment,
                    Group = g.Key.Alternative ? AlternativeUniformGroup : "UNIFORME",
                    Sizes = g.OrderBy(p => p.SizeOrder).ThenBy(p => p.Size)
                        .Select(p => new OrderFormSizeDto
                        {
                            ProductId = p.ProductId,
                            Code = p.Code,
                            Size = p.Size ?? "Única",
                            Price = p.Price,
                            Stock = p.Stock
                        })
                        .ToList()
                })
                .ToList()
        };

        return Responses.Ok(form);
    }

    private static List<OrderFormProductDto> ToFormProducts(IEnumerable<Product> products, string section) =>
        products
            .Where(p => p.Section == section)
            .OrderBy(p => p.Name)
            .Select(p => new OrderFormProductDto
            {
                ProductId = p.ProductId,
                Code = p.Code,
                Name = p.Name,
                Price = p.Price,
                Stock = p.Stock
            })
            .ToList();

    // ------------------------------------------------------------------ colegios

    public async Task<ResponseGetObject> GetSchools(bool includeInactive)
    {
        var schools = await _unitOfWork.Repository<School>().Query()
            .Where(s => includeInactive || s.IsActive)
            .OrderBy(s => s.DisplayOrder).ThenBy(s => s.Name)
            .ToListAsync();
        return Responses.Ok(schools.Select(ToDto).ToList());
    }

    public async Task<ResponsePost> SaveSchool(long? id, SchoolDto schoolDto)
    {
        var name = schoolDto.Name?.Trim() ?? string.Empty;
        if (name.Length < FieldRules.SchoolNameMin || name.Length > FieldRules.SchoolNameMax || !FieldRules.IsName(name))
        {
            return Responses.PostError(id ?? 0, HttpStatusCode.BadRequest,
                $"Nombre del colegio: entre {FieldRules.SchoolNameMin} y {FieldRules.SchoolNameMax} caracteres, con letras.");
        }

        var repository = _unitOfWork.Repository<School>();
        if (await repository.Query().AnyAsync(s => s.Name.ToLower() == name.ToLower() && s.SchoolId != (id ?? 0)))
        {
            return Responses.PostError(id ?? 0, HttpStatusCode.BadRequest, "Ya existe un colegio con ese nombre.");
        }

        School? school;
        if (id.HasValue)
        {
            school = await repository.GetByIdAsync(id.Value);
            if (school is null)
            {
                return Responses.PostError(id.Value, HttpStatusCode.NotFound, "Colegio no encontrado.");
            }
        }
        else
        {
            school = new School();
            await repository.AddAsync(school);
        }

        school.Name = name;
        school.DisplayOrder = schoolDto.DisplayOrder;
        school.IsActive = schoolDto.IsActive;
        await _unitOfWork.SaveChangesAsync();

        return Responses.Saved(school.SchoolId, "Colegio guardado.", id.HasValue ? HttpStatusCode.OK : HttpStatusCode.Created);
    }

    private static SchoolDto ToDto(School school) => new()
    {
        SchoolId = school.SchoolId,
        Name = school.Name,
        DisplayOrder = school.DisplayOrder,
        IsActive = school.IsActive
    };

    // ------------------------------------------------------------------ cursos y su lista de textos

    public async Task<ResponseGetObject> GetCourses()
    {
        var counts = await _unitOfWork.Repository<CourseProduct>().Query()
            .GroupBy(cp => cp.CourseId)
            .Select(g => new { CourseId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CourseId, x => x.Count);

        var courses = await _unitOfWork.Repository<Course>().Query()
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();

        return Responses.Ok(courses.Select(c => new CourseDto
        {
            CourseId = c.CourseId,
            Name = c.Name,
            Level = c.Level,
            DisplayOrder = c.DisplayOrder,
            IsActive = c.IsActive,
            ItemCount = counts.GetValueOrDefault(c.CourseId)
        }).ToList());
    }

    public async Task<ResponseGetObject> GetCourseProducts(long courseId)
    {
        var items = await (
                from cp in _unitOfWork.Repository<CourseProduct>().Query()
                join p in _unitOfWork.Repository<Product>().Query() on cp.ProductId equals p.ProductId
                where cp.CourseId == courseId
                orderby cp.DisplayOrder
                select new CourseProductDto
                {
                    ProductId = p.ProductId,
                    Code = p.Code,
                    Name = p.Name,
                    Price = p.Price,
                    IsDefaultSelected = cp.IsDefaultSelected
                })
            .ToListAsync();

        return Responses.Ok(items);
    }

    public async Task<ResponsePost> UpdateCourseProducts(long courseId, CourseProductsUpdateDto dto)
    {
        if (await _unitOfWork.Repository<Course>().GetByIdAsync(courseId) is null)
        {
            return Responses.PostError(courseId, HttpStatusCode.NotFound, "Curso no encontrado.");
        }

        var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
        var existing = await _unitOfWork.Repository<Product>().Query()
            .Where(p => productIds.Contains(p.ProductId))
            .Select(p => p.ProductId)
            .ToListAsync();
        if (existing.Count != productIds.Count)
        {
            return Responses.PostError(courseId, HttpStatusCode.BadRequest, "Hay productos que no existen en el catálogo.");
        }

        var repository = _unitOfWork.Repository<CourseProduct>();
        foreach (var old in await repository.Query().Where(cp => cp.CourseId == courseId).ToListAsync())
        {
            repository.Remove(old);
        }

        var order = 0;
        foreach (var item in dto.Items.DistinctBy(i => i.ProductId))
        {
            await repository.AddAsync(new CourseProduct
            {
                CourseId = courseId,
                ProductId = item.ProductId,
                DisplayOrder = ++order,
                IsDefaultSelected = item.IsDefaultSelected
            });
        }

        await _unitOfWork.SaveChangesAsync();
        return Responses.Saved(courseId, "Lista del curso guardada.");
    }

    // ------------------------------------------------------------------ productos y precios

    public async Task<ResponseGetObject> GetProducts(PaginationQueryFilter pagination, ProductQueryFilter filter)
    {
        var pageSize = pagination.PageSize > 0 ? pagination.PageSize : _paginationOptions.InitialPageSize;
        var pageNumber = pagination.PageNumber > 0 ? pagination.PageNumber : _paginationOptions.InitialPageNumber;

        var query = _unitOfWork.Repository<Product>().Query();
        if (!string.IsNullOrWhiteSpace(filter.Section))
        {
            query = query.Where(p => p.Section == filter.Section);
        }
        if (filter.IsActive.HasValue)
        {
            query = query.Where(p => p.IsActive == filter.IsActive.Value);
        }
        if (filter.WithoutPrice == true)
        {
            query = query.Where(p => p.Price <= 0);
        }
        if (!string.IsNullOrWhiteSpace(pagination.SearchCriteria))
        {
            var search = pagination.SearchCriteria.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(search) || (p.Code != null && p.Code.Contains(search)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(p => p.Section).ThenBy(p => p.GarmentName).ThenBy(p => p.SizeOrder).ThenBy(p => p.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Responses.Ok(new PagedResult<ProductDto>
        {
            Items = items.Select(ToDto),
            TotalRecords = total,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    public async Task<ResponsePost> SaveProduct(long? id, ProductDto dto)
    {
        var errors = new List<string>();
        var name = dto.Name.Trim();
        FieldRules.CheckLength(errors, name, "Nombre", FieldRules.ProductNameMin, FieldRules.ProductNameMax, required: true);
        if (name.Length > 0 && !FieldRules.IsName(name)) errors.Add("Nombre: solo letras, números, espacios y . , & ' -");
        if (!ProductSections.All.Contains(dto.Section)) errors.Add("La sección no es válida.");
        if (!FieldRules.IsValidPrice(dto.Price)) errors.Add($"Precio: entre 0 y {FieldRules.PriceMax:N0}, con hasta 2 decimales.");
        if (dto.Stock < 0 || decimal.Truncate(dto.Stock) != dto.Stock) errors.Add("Stock: número entero, 0 o más.");
        var rawCode = dto.Code?.Trim();
        if (!string.IsNullOrEmpty(rawCode) && (!FieldRules.IsDigits(rawCode) || rawCode.Length > FieldRules.ProductCodeMax))
            errors.Add($"Código: solo números, máximo {FieldRules.ProductCodeMax} dígitos.");
        if (dto.Section == ProductSections.Uniform)
        {
            FieldRules.CheckLength(errors, dto.GarmentName, "Prenda", FieldRules.GarmentMin, FieldRules.GarmentMax, required: false);
            if (!string.IsNullOrWhiteSpace(dto.GarmentName) && !FieldRules.IsLetters(dto.GarmentName.Trim()))
                errors.Add("Prenda: solo letras.");
            FieldRules.CheckLength(errors, dto.Size, "Talla", 0, FieldRules.SizeMax, required: false);
        }
        if (dto.Section == ProductSections.Uniform && string.IsNullOrWhiteSpace(dto.GarmentName))
            errors.Add("Indique la prenda del uniforme.");
        if (errors.Count > 0)
        {
            return Responses.PostError(id ?? 0, HttpStatusCode.BadRequest, errors.ToArray());
        }

        var code = string.IsNullOrWhiteSpace(dto.Code) ? null : dto.Code.Trim();
        var repository = _unitOfWork.Repository<Product>();
        if (code is not null && await repository.Query().AnyAsync(p => p.Code == code && p.ProductId != (id ?? 0)))
        {
            return Responses.PostError(id ?? 0, HttpStatusCode.BadRequest, $"El código {code} ya está en otro producto.");
        }

        Product? product;
        if (id.HasValue)
        {
            product = await repository.GetByIdAsync(id.Value);
            if (product is null)
            {
                return Responses.PostError(id.Value, HttpStatusCode.NotFound, "Producto no encontrado.");
            }
            product.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            product = new Product { CreatedAt = DateTime.UtcNow };
            await repository.AddAsync(product);
        }

        product.Code = code;
        product.Name = name;
        product.Section = dto.Section;
        product.AccountGroup = dto.AccountGroup;
        product.GarmentName = dto.Section == ProductSections.Uniform ? dto.GarmentName?.Trim().ToUpperInvariant() : null;
        product.Size = dto.Section == ProductSections.Uniform ? dto.Size?.Trim().ToUpperInvariant() : null;
        product.SizeOrder = dto.SizeOrder;
        product.Price = Math.Round(dto.Price, 2);
        product.Stock = dto.Stock;
        product.IsActive = dto.IsActive;

        await _unitOfWork.SaveChangesAsync();
        return Responses.Saved(product.ProductId, "Producto guardado.", id.HasValue ? HttpStatusCode.OK : HttpStatusCode.Created);
    }

    public async Task<ResponsePost> UpdatePrices(List<ProductPriceDto> prices)
    {
        if (prices.Any(p => !FieldRules.IsValidPrice(p.Price)))
        {
            return Responses.PostError(0, HttpStatusCode.BadRequest, $"Los precios van de 0 a {FieldRules.PriceMax:N0}, con hasta 2 decimales.");
        }

        var ids = prices.Select(p => p.ProductId).ToList();
        var products = await _unitOfWork.Repository<Product>().Tracked()
            .Where(p => ids.Contains(p.ProductId))
            .ToDictionaryAsync(p => p.ProductId);

        foreach (var price in prices)
        {
            if (products.TryGetValue(price.ProductId, out var product))
            {
                product.Price = Math.Round(price.Price, 2);
                product.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _unitOfWork.SaveChangesAsync();
        return Responses.Saved(0, $"{products.Count} precios actualizados.");
    }

    private static ProductDto ToDto(Product p) => new()
    {
        ProductId = p.ProductId,
        Code = p.Code,
        Name = p.Name,
        Section = p.Section,
        AccountGroup = p.AccountGroup,
        GarmentName = p.GarmentName,
        Size = p.Size,
        SizeOrder = p.SizeOrder,
        Price = p.Price,
        UnitCost = p.UnitCost,
        Stock = p.Stock,
        IsActive = p.IsActive
    };
}
