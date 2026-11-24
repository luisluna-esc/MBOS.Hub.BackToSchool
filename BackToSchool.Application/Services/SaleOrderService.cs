using System.Net;
using BackToSchool.Application.Dtos.Sales;
using BackToSchool.Application.Interfaces.Sales;
using BackToSchool.Application.Interfaces.Security;
using BackToSchool.core.Entities.Catalog;
using BackToSchool.core.Entities.ResponseApi.DisplayFormat;
using BackToSchool.core.Entities.Sales;
using BackToSchool.core.Entities.Security;
using BackToSchool.core.Interfaces;
using BackToSchool.core.QueryFilters.Sales;
using Microsoft.EntityFrameworkCore;

namespace BackToSchool.Application.Services;

public class SaleOrderService : ISaleOrderService
{
    private const int MaxQuantity = 99;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public SaleOrderService(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    /// <summary>Roles que manejan la cola de almacén (imprimir, marcar listo, anular cualquier pedido).</summary>
    public static readonly string[] WarehouseRoles = ["M-BOS", "Administrador", "Almacén"];

    private bool IsWarehouse => WarehouseRoles.Any(_currentUser.IsInRole);

    // ------------------------------------------------------------------ crear

    public async Task<ResponseGetObject> Create(SaleOrderCreateDto dto)
    {
        var promoterId = _currentUser.AppUserId;
        if (promoterId is null)
        {
            return Responses.GetError(HttpStatusCode.Unauthorized, "Sesión no válida.");
        }

        var errors = new List<string>();
        var customerName = dto.CustomerName?.Trim() ?? string.Empty;
        FieldRules.CheckLength(errors, customerName, "Nombre o razón social", FieldRules.CustomerNameMin, FieldRules.CustomerNameMax, required: true);
        if (customerName.Length > 0 && !FieldRules.IsName(customerName))
            errors.Add("Nombre o razón social: solo letras, números, espacios y . , & ' -");

        var taxId = Clean(dto.TaxId);
        if (taxId is not null && (!FieldRules.IsDigits(taxId) || taxId.Length < FieldRules.TaxIdMin || taxId.Length > FieldRules.TaxIdMax))
            errors.Add($"CI/NIT: solo números, entre {FieldRules.TaxIdMin} y {FieldRules.TaxIdMax} dígitos.");

        var phone = Clean(dto.Phone);
        if (phone is not null && (!FieldRules.IsDigits(phone) || phone.Length != FieldRules.PhoneLength || phone[0] is not ('6' or '7')))
            errors.Add($"Celular: {FieldRules.PhoneLength} dígitos que empiezan con 6 o 7.");

        FieldRules.CheckLength(errors, dto.Notes, "Observaciones", 0, FieldRules.NotesMax, required: false);

        if (dto.Lines.Count == 0) errors.Add("El pedido no tiene productos.");
        if (dto.Lines.Any(l => l.Quantity < 1 || l.Quantity > MaxQuantity)) errors.Add($"La cantidad debe estar entre 1 y {MaxQuantity}.");
        if (dto.Lines.Any(l => (l.StudentLabel?.Trim().Length ?? 0) > FieldRules.StudentLabelMax))
            errors.Add($"El nombre del hijo admite máximo {FieldRules.StudentLabelMax} caracteres.");
        foreach (var manual in dto.Lines.Where(l => l.ProductId is null))
        {
            var description = manual.Description?.Trim() ?? string.Empty;
            if (description.Length < FieldRules.ManualTextMin || description.Length > FieldRules.ManualTextMax || !FieldRules.IsName(description))
                errors.Add($"Texto escrito a mano: entre {FieldRules.ManualTextMin} y {FieldRules.ManualTextMax} caracteres, con letras.");
            if (manual.UnitPrice is null || !FieldRules.IsValidPrice(manual.UnitPrice.Value))
                errors.Add($"Texto escrito a mano: precio entre 0 y {FieldRules.PriceMax:N0}, con hasta 2 decimales.");
        }
        errors = errors.Distinct().ToList();
        if (errors.Count > 0)
        {
            return Responses.GetError(HttpStatusCode.BadRequest, errors.ToArray());
        }

        // Precios del catálogo: siempre los del sistema, nunca los que manda el celular.
        var productIds = dto.Lines.Where(l => l.ProductId.HasValue).Select(l => l.ProductId!.Value).Distinct().ToList();
        var products = await _unitOfWork.Repository<Product>().Query()
            .Where(p => productIds.Contains(p.ProductId))
            .ToDictionaryAsync(p => p.ProductId);
        if (products.Count != productIds.Count)
        {
            return Responses.GetError(HttpStatusCode.BadRequest, "Hay productos que ya no existen. Actualice la pantalla.");
        }

        var courseIds = dto.Lines.Where(l => l.CourseId.HasValue).Select(l => l.CourseId!.Value).Distinct().ToList();
        var courses = await _unitOfWork.Repository<Course>().Query()
            .Where(c => courseIds.Contains(c.CourseId))
            .ToDictionaryAsync(c => c.CourseId, c => c.Name);

        School? school = null;
        if (dto.SchoolId.HasValue)
        {
            school = await _unitOfWork.Repository<School>().GetByIdAsync(dto.SchoolId.Value);
            if (school is null)
            {
                return Responses.GetError(HttpStatusCode.BadRequest, "El colegio no existe.");
            }
        }

        var order = new SaleOrder
        {
            CustomerName = customerName,
            TaxId = taxId,
            Phone = phone,
            SchoolId = school?.SchoolId,
            SchoolName = school?.Name,
            PromoterAppUserId = promoterId.Value,
            Status = SaleOrderStatuses.Pending,
            Notes = Clean(dto.Notes),
            CreatedAt = DateTime.UtcNow
        };

        var displayOrder = 0;
        foreach (var line in dto.Lines)
        {
            var product = line.ProductId.HasValue ? products[line.ProductId.Value] : null;
            var unitPrice = product?.Price ?? Math.Round(line.UnitPrice!.Value, 2);
            order.Lines.Add(new SaleOrderLine
            {
                ProductId = product?.ProductId,
                Code = product?.Code,
                Description = product?.Name ?? line.Description!.Trim(),
                Size = product?.Size,
                Section = product?.Section ?? ProductSections.Text,
                CourseId = line.CourseId.HasValue && courses.ContainsKey(line.CourseId.Value) ? line.CourseId : null,
                CourseName = line.CourseId.HasValue ? courses.GetValueOrDefault(line.CourseId.Value) : null,
                StudentLabel = Clean(line.StudentLabel),
                Quantity = line.Quantity,
                UnitPrice = unitPrice,
                Subtotal = unitPrice * line.Quantity,
                IsManual = product is null,
                DisplayOrder = ++displayOrder
            });
        }
        order.Total = order.Lines.Sum(l => l.Subtotal);

        var today = DateOnly.FromDateTime(BoliviaTime.Now);
        await _unitOfWork.InTransactionAsync(async () =>
        {
            // Incremento atómico: aunque seis promotores envíen a la vez, ningún número se repite.
            var rows = await _unitOfWork.SqlQueryAsync<TicketNumberRow>(
                """
                INSERT INTO ticket_counter (ticket_date, last_number) VALUES ({0}, 1)
                ON CONFLICT (ticket_date) DO UPDATE SET last_number = ticket_counter.last_number + 1
                RETURNING last_number AS "LastNumber"
                """, today);
            order.TicketDate = today;
            order.TicketNumber = rows[0].LastNumber;

            await _unitOfWork.Repository<SaleOrder>().AddAsync(order);
            await _unitOfWork.SaveChangesAsync();
            return true;
        });

        return Responses.Ok(await ToDto(order, includeLines: true), HttpStatusCode.Created);
    }

    private sealed class TicketNumberRow
    {
        public int LastNumber { get; set; }
    }

    // ------------------------------------------------------------------ consultar

    public async Task<ResponseGetObject> GetAll(SaleOrderQueryFilter filter)
    {
        var date = filter.TicketDate ?? DateOnly.FromDateTime(BoliviaTime.Now);
        var query = _unitOfWork.Repository<SaleOrder>().Query()
            .Include(o => o.Lines)
            .Where(o => o.TicketDate == date);

        // Un promotor solo ve sus pedidos; almacén y administración ven todos.
        if (filter.Mine || !IsWarehouse && !_currentUser.IsInRole("Caja"))
        {
            var me = _currentUser.AppUserId ?? 0;
            query = query.Where(o => o.PromoterAppUserId == me);
        }
        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            query = query.Where(o => o.Status == filter.Status);
        }
        if (filter.TicketNumber.HasValue)
        {
            query = query.Where(o => o.TicketNumber == filter.TicketNumber.Value);
        }

        var orders = await query.OrderByDescending(o => o.TicketNumber).ToListAsync();
        var promoters = await PromoterNames(orders.Select(o => o.PromoterAppUserId));

        return Responses.Ok(orders.Select(o => ToDto(o, promoters, includeLines: false)).ToList());
    }

    public async Task<ResponseGetObject> GetById(long id)
    {
        var order = await _unitOfWork.Repository<SaleOrder>().Query()
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.SaleOrderId == id);

        if (order is null || !CanSee(order))
        {
            return Responses.GetError(HttpStatusCode.NotFound, "Pedido no encontrado.");
        }

        return Responses.Ok(await ToDto(order, includeLines: true));
    }

    public async Task<ResponseGetObject> GetSummary(DateOnly? ticketDate)
    {
        var date = ticketDate ?? DateOnly.FromDateTime(BoliviaTime.Now);
        var orders = await _unitOfWork.Repository<SaleOrder>().Query()
            .Where(o => o.TicketDate == date)
            .Select(o => new { o.Status, o.Total })
            .ToListAsync();

        return Responses.Ok(new SalesSummaryDto
        {
            TicketDate = date,
            Orders = orders.Count,
            Pending = orders.Count(o => o.Status == SaleOrderStatuses.Pending),
            Printed = orders.Count(o => o.Status == SaleOrderStatuses.Printed),
            Ready = orders.Count(o => o.Status == SaleOrderStatuses.Ready),
            Cancelled = orders.Count(o => o.Status == SaleOrderStatuses.Cancelled),
            Total = orders.Where(o => o.Status != SaleOrderStatuses.Cancelled).Sum(o => o.Total)
        });
    }

    // ------------------------------------------------------------------ estados

    public async Task<ResponsePost> ChangeStatus(long id, SaleOrderStatusDto dto)
    {
        var order = await _unitOfWork.Repository<SaleOrder>().Tracked()
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.SaleOrderId == id);
        if (order is null || !CanSee(order))
        {
            return Responses.PostError(id, HttpStatusCode.NotFound, "Pedido no encontrado.");
        }
        if (!SaleOrderStatuses.All.Contains(dto.Status))
        {
            return Responses.PostError(id, HttpStatusCode.BadRequest, "Estado no válido.");
        }
        if (order.Status == SaleOrderStatuses.Cancelled)
        {
            return Responses.PostError(id, HttpStatusCode.BadRequest, "El pedido ya está anulado.");
        }

        // El promotor solo puede anular su propio pedido mientras almacén no lo imprimió.
        var ownPendingCancel = dto.Status == SaleOrderStatuses.Cancelled
                               && order.Status == SaleOrderStatuses.Pending
                               && order.PromoterAppUserId == _currentUser.AppUserId;
        if (!IsWarehouse && !ownPendingCancel)
        {
            return Responses.PostError(id, HttpStatusCode.Forbidden,
                "Solo almacén puede cambiar este pedido. Si ya se imprimió, pida a almacén que lo anule.");
        }

        var wasReady = order.Status == SaleOrderStatuses.Ready;
        switch (dto.Status)
        {
            case SaleOrderStatuses.Pending:
                if (wasReady) await AdjustStock(order, +1);
                order.PrintedAt = null;
                order.ReadyAt = null;
                break;
            case SaleOrderStatuses.Printed:
                if (wasReady) await AdjustStock(order, +1);
                order.PrintedAt ??= DateTime.UtcNow;
                order.ReadyAt = null;
                break;
            case SaleOrderStatuses.Ready:
                if (!wasReady) await AdjustStock(order, -1);
                order.PrintedAt ??= DateTime.UtcNow;
                order.ReadyAt = DateTime.UtcNow;
                break;
            case SaleOrderStatuses.Cancelled:
                var reason = dto.Reason?.Trim() ?? string.Empty;
                if (reason.Length < FieldRules.CancelReasonMin || reason.Length > FieldRules.CancelReasonMax)
                {
                    return Responses.PostError(id, HttpStatusCode.BadRequest,
                        $"Indique el motivo de la anulación (entre {FieldRules.CancelReasonMin} y {FieldRules.CancelReasonMax} caracteres).");
                }
                if (wasReady) await AdjustStock(order, +1);
                order.CancelledAt = DateTime.UtcNow;
                order.CancelReason = dto.Reason.Trim();
                break;
        }

        order.Status = dto.Status;
        await _unitOfWork.SaveChangesAsync();
        return Responses.Saved(order.SaleOrderId, $"Ficha {order.TicketNumber} actualizada.");
    }

    /// <summary>El stock baja cuando almacén entrega los materiales a caja ("Listo") y vuelve si se revierte.</summary>
    private async Task AdjustStock(SaleOrder order, int direction)
    {
        var quantities = order.Lines
            .Where(l => l.ProductId.HasValue)
            .GroupBy(l => l.ProductId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));
        var ids = quantities.Keys.ToList();
        var products = await _unitOfWork.Repository<Product>().Tracked().Where(p => ids.Contains(p.ProductId)).ToListAsync();
        foreach (var product in products)
        {
            product.Stock += direction * quantities[product.ProductId];
        }
    }

    // ------------------------------------------------------------------ apoyo

    private bool CanSee(SaleOrder order) =>
        IsWarehouse || _currentUser.IsInRole("Caja") || order.PromoterAppUserId == _currentUser.AppUserId;

    private async Task<Dictionary<long, string>> PromoterNames(IEnumerable<long> ids)
    {
        var list = ids.Distinct().ToList();
        return await _unitOfWork.Repository<AppUser>().Query()
            .Where(u => list.Contains(u.AppUserId))
            .ToDictionaryAsync(u => u.AppUserId, u => (u.FirstName + " " + u.LastName).Trim());
    }

    private async Task<SaleOrderDto> ToDto(SaleOrder order, bool includeLines) =>
        ToDto(order, await PromoterNames([order.PromoterAppUserId]), includeLines);

    internal static SaleOrderDto ToDto(SaleOrder o, IReadOnlyDictionary<long, string> promoters, bool includeLines) => new()
    {
        SaleOrderId = o.SaleOrderId,
        TicketNumber = o.TicketNumber,
        TicketDate = o.TicketDate,
        CustomerName = o.CustomerName,
        TaxId = o.TaxId,
        Phone = o.Phone,
        SchoolId = o.SchoolId,
        SchoolName = o.SchoolName,
        PromoterAppUserId = o.PromoterAppUserId,
        PromoterName = promoters.GetValueOrDefault(o.PromoterAppUserId),
        Status = o.Status,
        Total = o.Total,
        ItemCount = o.Lines.Sum(l => l.Quantity),
        Notes = o.Notes,
        CreatedAt = o.CreatedAt,
        PrintedAt = o.PrintedAt,
        ReadyAt = o.ReadyAt,
        CancelledAt = o.CancelledAt,
        CancelReason = o.CancelReason,
        Lines = includeLines
            ? o.Lines.OrderBy(l => l.DisplayOrder).Select(l => new SaleOrderLineDto
            {
                SaleOrderLineId = l.SaleOrderLineId,
                ProductId = l.ProductId,
                Code = l.Code,
                Description = l.Description,
                Size = l.Size,
                Section = l.Section,
                CourseId = l.CourseId,
                CourseName = l.CourseName,
                StudentLabel = l.StudentLabel,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                Subtotal = l.Subtotal,
                IsManual = l.IsManual
            }).ToList()
            : null
    };

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
