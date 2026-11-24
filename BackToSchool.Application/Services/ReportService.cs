using BackToSchool.Application.Interfaces.Sales;
using BackToSchool.Application.Interfaces.Security;
using BackToSchool.core.Entities.Catalog;
using BackToSchool.core.Entities.Sales;
using BackToSchool.core.Entities.Security;
using BackToSchool.core.Interfaces;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BackToSchool.Application.Services;

public partial class ReportService : IReportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    static ReportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public ReportService(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    private static readonly Lazy<byte[]> LogoBytes = new(() =>
    {
        using var stream = typeof(ReportService).Assembly.GetManifestResourceStream("BackToSchool.Application.Resources.mbos-logo.png")
            ?? throw new InvalidOperationException("Logo embebido (Resources/mbos-logo.png) no encontrado.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    });

    private static byte[] GetLogoBytes() => LogoBytes.Value;

    public async Task<(byte[]? Pdf, string FileName)> GetSaleOrderPdf(long saleOrderId)
    {
        var order = await _unitOfWork.Repository<SaleOrder>().Tracked()
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.SaleOrderId == saleOrderId);
        if (order is null)
        {
            return (null, string.Empty);
        }

        // Imprimir la hoja es la señal de que almacén ya tomó el pedido.
        if (order.Status == SaleOrderStatuses.Pending)
        {
            order.Status = SaleOrderStatuses.Printed;
            order.PrintedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();
        }

        var users = await _unitOfWork.Repository<AppUser>().Query()
            .Where(u => u.AppUserId == order.PromoterAppUserId || u.AppUserId == _currentUser.AppUserId)
            .ToDictionaryAsync(u => u.AppUserId);
        var promoter = users.TryGetValue(order.PromoterAppUserId, out var p) ? $"{p.FirstName} {p.LastName}".Trim() : "—";
        var printedBy = _currentUser.AppUserId is { } me && users.TryGetValue(me, out var u) ? u.Username : "sistema";

        var pdf = Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(ReportInk));

                page.Header().Element(c => ReportHeaderWithoutCurrency(c, "Pedido de Venta",
                    $"Fecha: {BoliviaTime.FromUtc(order.CreatedAt):dd/MM/yyyy HH:mm}",
                    $"Promotor: {promoter}",
                    order.SchoolName is null ? null : $"Colegio: {order.SchoolName}",
                    order.Status == SaleOrderStatuses.Cancelled ? $"Estado: ANULADO — {order.CancelReason}" : null));

                page.Content().Column(column =>
                {
                    column.Spacing(8);
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Border(0.8f).BorderColor(ReportLineStrong).Padding(8).Column(c =>
                        {
                            c.Spacing(2);
                            c.Item().Text("DATOS PARA LA FACTURA").FontSize(7).SemiBold().FontColor(ReportMuted);
                            c.Item().Text(t => { t.Span("Razón social: ").FontColor(ReportMuted); t.Span(order.CustomerName).SemiBold(); });
                            c.Item().Text(t => { t.Span("CI/NIT: ").FontColor(ReportMuted); t.Span(order.TaxId ?? "—"); });
                            c.Item().Text(t => { t.Span("Celular: ").FontColor(ReportMuted); t.Span(order.Phone ?? "—"); });
                        });
                        row.ConstantItem(12);
                        row.ConstantItem(130).Background(ReportAccent).Padding(8).AlignCenter().AlignMiddle().Column(c =>
                        {
                            c.Item().AlignCenter().Text("FICHA N°").FontSize(8).SemiBold().FontColor(Colors.White);
                            c.Item().AlignCenter().Text(order.TicketNumber.ToString()).FontSize(34).Bold().FontColor(Colors.White);
                            c.Item().AlignCenter().Text($"{order.TicketDate:dd/MM/yyyy}").FontSize(7.5f).FontColor(Colors.White);
                        });
                    });

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(18);   // casilla para que almacén marque
                            cols.ConstantColumn(30);   // cantidad
                            cols.ConstantColumn(48);   // código
                            cols.RelativeColumn();     // descripción
                            cols.ConstantColumn(36);   // talla
                            cols.ConstantColumn(52);   // P/U
                            cols.ConstantColumn(60);   // subtotal
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("✓");
                            header.Cell().Element(HeaderCell).AlignCenter().Text("Cant.");
                            header.Cell().Element(HeaderCell).Text("Código");
                            header.Cell().Element(HeaderCell).Text("Descripción");
                            header.Cell().Element(HeaderCell).AlignCenter().Text("Talla");
                            header.Cell().Element(HeaderCell).AlignRight().Text("P/U");
                            header.Cell().Element(HeaderCell).AlignRight().Text("Subtotal");
                        });

                        foreach (var group in GroupLines(order.Lines))
                        {
                            table.Cell().ColumnSpan(7).Element(GroupTitleCell).Text(group.Title).Bold().FontSize(9.5f);
                            foreach (var line in group.Lines)
                            {
                                table.Cell().Element(BodyCell).AlignMiddle().Width(10).Height(10).Border(0.8f).BorderColor(ReportMuted);
                                table.Cell().Element(BodyCell).AlignCenter().Text(line.Quantity.ToString()).SemiBold();
                                table.Cell().Element(BodyCell).Text(line.Code ?? "").FontSize(8).FontColor(ReportMuted);
                                table.Cell().Element(BodyCell).Text(t =>
                                {
                                    t.Span(line.Description);
                                    if (line.IsManual) t.Span("  (escrito a mano)").FontSize(7).Italic().FontColor(ReportMuted);
                                });
                                table.Cell().Element(BodyCell).AlignCenter().Text(line.Size ?? "").SemiBold();
                                table.Cell().Element(BodyCell).AlignRight().Text($"{line.UnitPrice:N2}");
                                table.Cell().Element(BodyCell).AlignRight().Text($"{line.Subtotal:N2}");
                            }
                        }

                        table.Cell().ColumnSpan(5).Element(CategoryTotalCell).Text($"{order.Lines.Sum(l => l.Quantity)} artículos").SemiBold();
                        table.Cell().ColumnSpan(2).Element(CategoryTotalCell).AlignRight().Text($"Bs. {order.Total:N2}").Bold().FontSize(11);
                    });

                    if (order.Notes is not null)
                    {
                        column.Item().Background(ReportSoftFill).Padding(6).Text(t =>
                        {
                            t.Span("Observaciones: ").SemiBold();
                            t.Span(order.Notes);
                        });
                    }

                    column.Item().PaddingTop(24).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().LineHorizontal(0.6f).LineColor(ReportMuted);
                            c.Item().AlignCenter().Text("Entregado por almacén").FontSize(7.5f).FontColor(ReportMuted);
                        });
                        row.ConstantItem(40);
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().LineHorizontal(0.6f).LineColor(ReportMuted);
                            c.Item().AlignCenter().Text("Recibido en caja").FontSize(7.5f).FontColor(ReportMuted);
                        });
                    });
                });

                page.Footer().Element(c => ReportFooter(c, printedBy));
            });
        }).GeneratePdf();

        return (pdf, $"Ficha-{order.TicketNumber}-{order.TicketDate:yyyyMMdd}.pdf");
    }

    private sealed record LineGroup(string Title, List<SaleOrderLine> Lines);

    /// <summary>Textos por curso (y por hijo), luego cuadernos, uniformes y otros: el orden en que almacén los saca.</summary>
    private static IEnumerable<LineGroup> GroupLines(IEnumerable<SaleOrderLine> lines)
    {
        var all = lines.OrderBy(l => l.DisplayOrder).ToList();

        foreach (var course in all.Where(l => l.Section == ProductSections.Text)
                     .GroupBy(l => (l.StudentLabel, l.CourseName)))
        {
            var title = course.Key.CourseName is null ? "Textos" : $"Textos · {course.Key.CourseName}";
            if (course.Key.StudentLabel is not null) title += $" ({course.Key.StudentLabel})";
            yield return new LineGroup(title, course.ToList());
        }

        foreach (var (section, title) in new[]
                 {
                     (ProductSections.Notebook, "Cuadernos"),
                     (ProductSections.Uniform, "Uniformes"),
                     (ProductSections.Other, "Otros materiales")
                 })
        {
            var sectionLines = all.Where(l => l.Section == section).ToList();
            if (sectionLines.Count > 0)
            {
                yield return new LineGroup(title, sectionLines);
            }
        }
    }
}
