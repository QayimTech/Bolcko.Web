using Blocko.Services.Interfaces.Delivery;
using Bolcko.Domain.Entities.Delivery;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;
using ClosedXML.Excel;

namespace Blocko.Services.Implementations.Delivery
{
    public class DeliveryDocumentService : IDeliveryDocumentService
    {
        static DeliveryDocumentService()
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        }

        public byte[] GenerateExcelSheet(DeliveryJob job)
        {
            var order = job.Order;
            var customer = order?.User;
            var address = order?.ShippingAddress;
            var driver = job.Driver;
            var company = driver?.Company;

            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("تفاصيل التوصيل");
                ws.RightToLeft = true;

                // Title
                ws.Cell(1, 1).Value = "تفاصيل مهمة التوصيل - بولكو";
                ws.Cell(1, 1).Style.Font.Bold = true;
                ws.Cell(1, 1).Style.Font.FontSize = 16;
                ws.Range(1, 1, 1, 6).Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Info block
                ws.Cell(3, 1).Value = "رقم المهمة:";
                ws.Cell(3, 1).Style.Font.Bold = true;
                ws.Cell(3, 2).Value = $"#{job.Id}";

                ws.Cell(3, 4).Value = "رقم الطلب:";
                ws.Cell(3, 4).Style.Font.Bold = true;
                ws.Cell(3, 5).Value = order?.OrderNumber ?? "-";

                ws.Cell(4, 1).Value = "تاريخ المهمة:";
                ws.Cell(4, 1).Style.Font.Bold = true;
                ws.Cell(4, 2).Value = DateTime.UtcNow.ToString("yyyy/MM/dd");

                ws.Cell(4, 4).Value = "شركة التوصيل:";
                ws.Cell(4, 4).Style.Font.Bold = true;
                ws.Cell(4, 5).Value = company?.Name ?? "مندوب مستقل";

                // Addresses
                ws.Cell(6, 1).Value = "موقع الاستلام:";
                ws.Cell(6, 1).Style.Font.Bold = true;
                ws.Cell(6, 2).Value = job.PickupLocation;

                ws.Cell(7, 1).Value = "موقع التسليم:";
                ws.Cell(7, 1).Style.Font.Bold = true;
                ws.Cell(7, 2).Value = job.DropoffLocation;

                // Customer info
                ws.Cell(9, 1).Value = "تفاصيل المستلم:";
                ws.Cell(9, 1).Style.Font.Bold = true;
                ws.Cell(9, 1).Style.Font.FontSize = 12;

                ws.Cell(10, 1).Value = "الاسم:";
                ws.Cell(10, 2).Value = $"{customer?.FirstName} {customer?.LastName}";
                ws.Cell(11, 1).Value = "الهاتف:";
                ws.Cell(11, 2).Value = customer?.PhoneNumber ?? "-";
                ws.Cell(12, 1).Value = "العنوان بالتفصيل:";
                ws.Cell(12, 2).Value = $"{address?.City}، {address?.AddressLine1} {(string.IsNullOrEmpty(address?.AddressLine2) ? "" : "، " + address.AddressLine2)}";

                // Items Table header
                int startRow = 14;
                ws.Cell(startRow, 1).Value = "المنتج";
                ws.Cell(startRow, 2).Value = "رمز المنتج SKU";
                ws.Cell(startRow, 3).Value = "الكمية";
                ws.Cell(startRow, 4).Value = "سعر الوحدة";
                ws.Cell(startRow, 5).Value = "الإجمالي";

                var headerRange = ws.Range(startRow, 1, startRow, 5);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E293B");
                headerRange.Style.Font.FontColor = XLColor.White;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                int currentRow = startRow + 1;
                if (order?.Items != null)
                {
                    foreach (var item in order.Items)
                    {
                        ws.Cell(currentRow, 1).Value = item.Product?.Name ?? "-";
                        ws.Cell(currentRow, 2).Value = item.Product?.Sku ?? "-";
                        ws.Cell(currentRow, 3).Value = item.Quantity;
                        ws.Cell(currentRow, 4).Value = item.UnitPrice;
                        ws.Cell(currentRow, 4).Style.NumberFormat.Format = "$#,##0.00";
                        ws.Cell(currentRow, 5).Value = item.Subtotal;
                        ws.Cell(currentRow, 5).Style.NumberFormat.Format = "$#,##0.00";

                        // Borders
                        ws.Range(currentRow, 1, currentRow, 5).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                        ws.Range(currentRow, 1, currentRow, 5).Style.Border.BottomBorderColor = XLColor.LightGray;

                        currentRow++;
                    }
                }

                // Summary
                currentRow += 1;
                var subtotal = order?.TotalAmount ?? 0;
                var fee = job.DeliveryFee;
                var discount = order?.DiscountAmount ?? 0;
                var grandTotal = subtotal + fee - discount;

                ws.Cell(currentRow, 4).Value = "مجموع المنتجات:";
                ws.Cell(currentRow, 4).Style.Font.Bold = true;
                ws.Cell(currentRow, 5).Value = subtotal;
                ws.Cell(currentRow, 5).Style.NumberFormat.Format = "$#,##0.00";

                if (discount > 0)
                {
                    currentRow++;
                    ws.Cell(currentRow, 4).Value = "الخصم:";
                    ws.Cell(currentRow, 4).Style.Font.Bold = true;
                    ws.Cell(currentRow, 5).Value = discount;
                    ws.Cell(currentRow, 5).Style.NumberFormat.Format = "$#,##0.00";
                }

                currentRow++;
                ws.Cell(currentRow, 4).Value = "رسوم التوصيل:";
                ws.Cell(currentRow, 4).Style.Font.Bold = true;
                ws.Cell(currentRow, 5).Value = fee;
                ws.Cell(currentRow, 5).Style.NumberFormat.Format = "$#,##0.00";

                currentRow++;
                ws.Cell(currentRow, 4).Value = "الإجمالي الكلي:";
                ws.Cell(currentRow, 4).Style.Font.Bold = true;
                ws.Cell(currentRow, 5).Value = grandTotal;
                ws.Cell(currentRow, 5).Style.Font.Bold = true;
                ws.Cell(currentRow, 5).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(currentRow, 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF08A"); // Yellow accent

                ws.Columns().AdjustToContents();

                using (var ms = new MemoryStream())
                {
                    workbook.SaveAs(ms);
                    return ms.ToArray();
                }
            }
        }

        public byte[] GeneratePdfDocument(DeliveryJob job)
        {
            var order = job.Order;
            var customer = order?.User;
            var address = order?.ShippingAddress;
            var driver = job.Driver;
            var company = driver?.Company;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                    page.Header().Row(row =>
                    {
                        // Right side: Logo
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Row(logoRow =>
                            {
                                // Draw yellow block icon
                                logoRow.ConstantItem(35).Height(35).Background("#E8A020").Padding(6).Row(innerRow =>
                                {
                                    innerRow.ConstantItem(5).Background(Colors.White);
                                    innerRow.ConstantItem(3);
                                    innerRow.RelativeItem().Column(innerCol =>
                                    {
                                        innerCol.Item().Height(8).Border(1.5f).BorderColor(Colors.White);
                                        innerCol.Item().Height(3);
                                        innerCol.Item().Height(8).Border(1.5f).BorderColor(Colors.White);
                                    });
                                });
                                logoRow.ConstantItem(10);
                                logoRow.RelativeItem().Column(textCol =>
                                {
                                    textCol.Item().Text("BOLCKO").Bold().FontSize(22).FontColor("#1E293B");
                                    textCol.Item().Text("CONSTRUCTION SUPPLIES").FontSize(6).Bold().FontColor("#E8A020").LetterSpacing(0.2f);
                                });
                            });
                        });

                        // Left side: Document Info (RTL)
                        row.RelativeItem().AlignLeft().ContentFromRightToLeft().Column(col =>
                        {
                            col.Item().Text("مستند توصيل طلب").Bold().FontSize(18).FontColor("#1E293B");
                            col.Item().Text($"رقم المهمة: #{job.Id}").FontSize(10).FontColor("#64748B");
                            col.Item().Text($"تاريخ المهمة: {DateTime.UtcNow:yyyy/MM/dd}").FontSize(10).FontColor("#64748B");
                        });
                    });

                    page.Content().PaddingVertical(1.5f, Unit.Centimetre).ContentFromRightToLeft().Column(col =>
                    {
                        col.Spacing(20);

                        // Two column metadata section (RTL layout)
                        col.Item().Row(metaRow =>
                        {
                            metaRow.Spacing(20);

                            // Left Column: Customer & Delivery Info
                            metaRow.RelativeItem().Border(1).BorderColor("#E2E8F0").Padding(12).Column(c =>
                            {
                                c.Spacing(6);
                                c.Item().Text("تفاصيل العميل والتسليم").Bold().FontSize(12).FontColor("#1E293B");
                                c.Item().LineHorizontal(1).LineColor("#E2E8F0");
                                c.Item().Text($"العميل: {customer?.FirstName} {customer?.LastName}");
                                c.Item().Text($"الهاتف: {customer?.PhoneNumber ?? "-"}");
                                c.Item().Text($"العنوان: {address?.City}، {address?.AddressLine1}");
                                if (!string.IsNullOrEmpty(address?.AddressLine2))
                                {
                                    c.Item().Text(address.AddressLine2);
                                }
                            });

                            // Right Column: Company & Order Info
                            metaRow.RelativeItem().Border(1).BorderColor("#E2E8F0").Padding(12).Column(c =>
                            {
                                c.Spacing(6);
                                c.Item().Text("تفاصيل الطلب وشركة التوصيل").Bold().FontSize(12).FontColor("#1E293B");
                                c.Item().LineHorizontal(1).LineColor("#E2E8F0");
                                c.Item().Text($"رقم الطلب: {order?.OrderNumber}");
                                c.Item().Text($"تاريخ الطلب: {order?.OrderDate:yyyy/MM/dd}");
                                c.Item().Text($"شركة التوصيل: {company?.Name ?? "مندوب مستقل"}");
                                if (driver != null)
                                {
                                    c.Item().Text($"المندوب: {driver.User?.FirstName} {driver.User?.LastName} ({driver.VehicleType} - {driver.VehiclePlateNumber})");
                                }
                            });
                        });

                        // Items table
                        col.Item().Column(tableCol =>
                        {
                            tableCol.Spacing(8);
                            tableCol.Item().Text("المنتجات المطلوبة").Bold().FontSize(12).FontColor("#1E293B");

                            tableCol.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3); // Product
                                    columns.RelativeColumn(2); // SKU
                                    columns.RelativeColumn(1); // Qty
                                    columns.RelativeColumn(1.5f); // Unit Price
                                    columns.RelativeColumn(1.5f); // Total
                                });

                                // Header row
                                table.Header(header =>
                                {
                                    header.Cell().Background("#1E293B").Padding(6).AlignCenter().Text("المنتج").Bold().FontColor(Colors.White);
                                    header.Cell().Background("#1E293B").Padding(6).AlignCenter().Text("رمز المنتج SKU").Bold().FontColor(Colors.White);
                                    header.Cell().Background("#1E293B").Padding(6).AlignCenter().Text("الكمية").Bold().FontColor(Colors.White);
                                    header.Cell().Background("#1E293B").Padding(6).AlignCenter().Text("سعر الوحدة").Bold().FontColor(Colors.White);
                                    header.Cell().Background("#1E293B").Padding(6).AlignCenter().Text("الإجمالي").Bold().FontColor(Colors.White);
                                });

                                if (order?.Items != null)
                                {
                                    bool alternate = false;
                                    foreach (var item in order.Items)
                                    {
                                        var bg = alternate ? "#F8FAFC" : "#FFFFFF";
                                        table.Cell().Background(bg).Padding(6).AlignRight().Text(item.Product?.Name ?? "-");
                                        table.Cell().Background(bg).Padding(6).AlignCenter().Text(item.Product?.Sku ?? "-");
                                        table.Cell().Background(bg).Padding(6).AlignCenter().Text(item.Quantity.ToString());
                                        table.Cell().Background(bg).Padding(6).AlignCenter().Text($"{item.UnitPrice:N2} د.أ");
                                        table.Cell().Background(bg).Padding(6).AlignCenter().Text($"{item.Subtotal:N2} د.أ");
                                        alternate = !alternate;
                                    }
                                }
                            });
                        });

                        // Pricing summary block
                        col.Item().AlignLeft().Width(250).Border(1).BorderColor("#E2E8F0").Padding(12).Column(sumCol =>
                        {
                            sumCol.Spacing(4);
                            var subtotal = order?.TotalAmount ?? 0;
                            var fee = job.DeliveryFee;
                            var discount = order?.DiscountAmount ?? 0;
                            var grandTotal = subtotal + fee - discount;

                            sumCol.Item().Row(r =>
                            {
                                r.RelativeItem().Text($"{subtotal:N2} د.أ");
                                r.RelativeItem().AlignRight().Text("مجموع المنتجات:").Bold();
                            });
                            if (discount > 0)
                            {
                                sumCol.Item().Row(r =>
                                {
                                    r.RelativeItem().Text($"-{discount:N2} د.أ");
                                    r.RelativeItem().AlignRight().Text("الخصم:").Bold();
                                });
                            }
                            sumCol.Item().Row(r =>
                            {
                                r.RelativeItem().Text($"{fee:N2} د.أ");
                                r.RelativeItem().AlignRight().Text("رسوم التوصيل:").Bold();
                            });
                            sumCol.Item().LineHorizontal(1).LineColor("#E2E8F0");
                            sumCol.Item().Row(r =>
                            {
                                r.RelativeItem().Text($"{grandTotal:N2} د.أ").Bold().FontColor("#E8A020");
                                r.RelativeItem().AlignRight().Text("الإجمالي الكلي:").Bold();
                            });
                        });
                    });

                    page.Footer().AlignCenter().Column(foot =>
                    {
                        foot.Item().LineHorizontal(1).LineColor("#E2E8F0");
                        foot.Item().PaddingTop(10).Text("شكرًا لتعاملكم معنا - بولكو للوازم البناء والتوصيل").FontSize(8).FontColor("#94A3B8");
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
