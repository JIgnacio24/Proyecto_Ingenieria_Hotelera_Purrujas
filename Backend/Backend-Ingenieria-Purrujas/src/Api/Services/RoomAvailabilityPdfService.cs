using Backend_Ingenieria_Purrujas.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Backend_Ingenieria_Purrujas.Api.Services;

public sealed record RoomAvailabilityPdfDocument(byte[] Content, string FileName);

public sealed class RoomAvailabilityPdfService
{
    private const string ReportTitle = "Reporte de estado de habitaciones";
    private const string UnavailableLabel = "No disponible";
    private const string EmptyRoomsLabel = "No hay habitaciones registradas al momento de generar este reporte.";

    static RoomAvailabilityPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public RoomAvailabilityPdfDocument Generate(
        RoomAvailabilitySummary summary,
        AdminUser generatedBy,
        DateTime generatedAt,
        string systemName)
    {
        var fileName = $"reporte_estado_habitaciones_{generatedAt:yyyy-MM-dd_HH-mm}.pdf";
        var pdfBytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.4f, Unit.Centimetre);
                page.DefaultTextStyle(text => text.FontSize(10).FontColor(Colors.Grey.Darken4));

                page.Header().Column(column =>
                {
                    column.Spacing(6);
                    column.Item().Text(systemName)
                        .FontSize(11)
                        .SemiBold()
                        .FontColor(Colors.Green.Darken3);
                    column.Item().Text(ReportTitle)
                        .FontSize(18)
                        .Bold()
                        .FontColor(Colors.Grey.Darken4);
                    column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().Column(column =>
                {
                    column.Spacing(12);

                    column.Item().Column(info =>
                    {
                        info.Spacing(6);
                        info.Item().Text("Datos de generacion").SemiBold().FontColor(Colors.Green.Darken3);
                        info.Item().Text($"Fecha de generacion: {generatedAt:yyyy-MM-dd}");
                        info.Item().Text($"Hora de generacion: {generatedAt:HH:mm:ss}");
                        info.Item().Text($"Usuario: {ResolveUserLabel(generatedBy)}");
                        info.Item().Text($"Fecha consultada: {summary.Date:yyyy-MM-dd}");
                        info.Item().Text($"Total habitaciones: {summary.TotalRooms}");
                        info.Item().Text($"Disponibles: {summary.AvailableRooms}");
                    });

                    column.Item().PaddingTop(2).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Cell().Element(MetricCellStyle).Column(metric =>
                        {
                            metric.Spacing(4);
                            metric.Item().Text("Disponibles").FontColor(Colors.Green.Darken3).SemiBold();
                            metric.Item().Text(summary.AvailableRooms.ToString()).FontSize(18).Bold();
                        });

                        table.Cell().Element(MetricCellStyle).Column(metric =>
                        {
                            metric.Spacing(4);
                            metric.Item().Text("Ocupadas").FontColor(Colors.Amber.Darken3).SemiBold();
                            metric.Item().Text(summary.OccupiedRooms.ToString()).FontSize(18).Bold();
                        });

                        table.Cell().Element(MetricCellStyle).Column(metric =>
                        {
                            metric.Spacing(4);
                            metric.Item().Text("Fuera de servicio").FontColor(Colors.Red.Darken2).SemiBold();
                            metric.Item().Text(summary.OutOfServiceRooms.ToString()).FontSize(18).Bold();
                        });
                    });

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.1f);
                            columns.RelativeColumn(1.4f);
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(1.8f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCellStyle).Text("Habitacion");
                            header.Cell().Element(HeaderCellStyle).Text("Tipo");
                            header.Cell().Element(HeaderCellStyle).Text("Estado");
                            header.Cell().Element(HeaderCellStyle).Text("Huesped actual");
                        });

                        if (summary.Rooms.Count == 0)
                        {
                            table.Cell().ColumnSpan(4).Element(BodyCellStyle).Text(EmptyRoomsLabel);
                            return;
                        }

                        foreach (var room in summary.Rooms)
                        {
                            table.Cell().Element(BodyCellStyle).Text(ResolveText(room.RoomNumber));
                            table.Cell().Element(BodyCellStyle).Text(ResolveText(room.RoomTypeName));
                            table.Cell().Element(BodyCellStyle).Text(ResolveText(room.StatusName));
                            table.Cell().Element(BodyCellStyle).Text(ResolveText(room.CurrentGuest, "Sin huesped"));
                        }
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Pagina ");
                    text.CurrentPageNumber();
                    text.Span(" de ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();

        return new RoomAvailabilityPdfDocument(pdfBytes, fileName);
    }

    private static string ResolveUserLabel(AdminUser user)
    {
        var parts = new[]
        {
            string.IsNullOrWhiteSpace(user.FullName) ? null : user.FullName.Trim(),
            string.IsNullOrWhiteSpace(user.Username) ? null : $"@{user.Username.Trim()}",
            string.IsNullOrWhiteSpace(user.Email) ? null : user.Email.Trim()
        }
        .Where(value => !string.IsNullOrWhiteSpace(value));

        var label = string.Join(" | ", parts);
        return string.IsNullOrWhiteSpace(label) ? UnavailableLabel : label;
    }

    private static string ResolveText(string? value, string fallback = UnavailableLabel)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static IContainer MetricCellStyle(IContainer container)
    {
        return container
            .PaddingVertical(6)
            .PaddingHorizontal(8)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2);
    }

    private static IContainer HeaderCellStyle(IContainer container)
    {
        return container
            .Background(Colors.Grey.Lighten3)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten1)
            .PaddingVertical(8)
            .PaddingHorizontal(10);
    }

    private static IContainer BodyCellStyle(IContainer container)
    {
        return container
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten3)
            .PaddingVertical(8)
            .PaddingHorizontal(10);
    }
}
