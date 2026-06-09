using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using D = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using SmartphoneShop.Web.Models;

namespace SmartphoneShop.Web.Services;

public class ReportGenerator
{
    private const int ImgWidth = 600;
    private const int ImgHeight = 380;
    private const long EmuPerPixel = 914400L / 96L;

    public byte[] GenerateReport(AdminReportViewModel data)
    {
        var stream = new MemoryStream();
        {
            using var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = new Body();

            var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
            stylesPart.Styles = CreateStyles();
            stylesPart.Styles.Save();

            AddTitle(body);
            AddSalesOverview(body, data);
            AddChartSection(mainPart, body, "2. Статусы заказов", ChartDrawer.DrawPieChart("Статусы заказов",
                data.OrdersByStatus.Select(s => s.Label).ToArray(),
                data.OrdersByStatus.Select(s => s.Count).ToArray()),
                ["Статус", "Количество"],
                data.OrdersByStatus.Select(s => new[] { s.Label, s.Count.ToString("N0") }).ToArray());

            AddChartSection(mainPart, body, "3. Выручка по месяцам", ChartDrawer.DrawColumnChart("Выручка по месяцам, BYN",
                data.MonthlyRevenue.Select(m => m.Label).ToArray(),
                data.MonthlyRevenue.Select(m => (int)m.Value).ToArray()),
                ["Месяц", "Выручка", "Заказы"],
                data.MonthlyRevenue.Select(m => new[] { m.Label, m.Value.ToString("N0") + " BYN", m.Count.ToString() }).ToArray());

            AddChartSection(mainPart, body, "4. Топ-10 товаров", ChartDrawer.DrawBarChart("Товары (кол-во)",
                data.TopSellingProducts.Select(p => p.Name).ToArray(),
                data.TopSellingProducts.Select(p => p.Quantity).ToArray()),
                ["Товар", "Продано", "Выручка"],
                data.TopSellingProducts.Select(p => new[] { p.Name, p.Quantity.ToString("N0"), p.Revenue.ToString("N0") + " BYN" }).ToArray());

            AddChartSection(mainPart, body, "5. Популярные бренды", ChartDrawer.DrawPieChart("Доля брендов",
                data.BrandSales.Select(b => b.Name).ToArray(),
                data.BrandSales.Select(b => b.Quantity).ToArray()),
                ["Бренд", "Продано", "Выручка"],
                data.BrandSales.Select(b => new[] { b.Name, b.Quantity.ToString("N0"), b.Revenue.ToString("N0") + " BYN" }).ToArray());

            AddUnsoldProducts(body, data);

            AddChartSection(mainPart, body, "7. Ремонты — статусы", ChartDrawer.DrawPieChart("Статусы ремонтов",
                data.RepairsByStatus.Select(s => s.Label).ToArray(),
                data.RepairsByStatus.Select(s => s.Count).ToArray()),
                ["Статус", "Количество"],
                data.RepairsByStatus.Select(s => new[] { s.Label, s.Count.ToString("N0") }).ToArray());

            AddChartSection(mainPart, body, "7.1 Топ-5 моделей для ремонта", ChartDrawer.DrawBarChart("Количество заявок",
                data.TopRepairModels.Select(p => p.Name).ToArray(),
                data.TopRepairModels.Select(p => p.Quantity).ToArray()),
                ["Модель", "Заявок"],
                data.TopRepairModels.Select(p => new[] { p.Name, p.Quantity.ToString("N0") }).ToArray());

            AddChartSection(mainPart, body, "8. Новые пользователи", ChartDrawer.DrawColumnChart("Новые пользователи",
                data.NewUsersByMonth.Select(m => m.Label).ToArray(),
                data.NewUsersByMonth.Select(m => m.Count).ToArray()),
                ["Месяц", "Новых пользователей"],
                data.NewUsersByMonth.Select(m => new[] { m.Label, m.Count.ToString("N0") }).ToArray());

            AddFooter(body);

            mainPart.Document.Body = body;
            mainPart.Document.Save();
        }
        var result = stream.ToArray();
        stream.Dispose();
        return result;
    }

    private void AddChartSection(MainDocumentPart mainPart, Body body, string title, byte[] chartPng, string[] headers, string[][] rows)
    {
        body.Append(MakePara(title, "Heading2", true, "D44177", "28"));
        if (chartPng.Length > 0)
        {
            var imagePart = mainPart.AddImagePart(ImagePartType.Png);
            imagePart.FeedData(new MemoryStream(chartPng));
            var relId = mainPart.GetIdOfPart(imagePart);
            body.Append(new Paragraph(new Run(CreateImageDrawing(relId))));
        }
        if (rows.Length > 0)
        {
            body.Append(CreateTable(headers, rows));
        }
        else
        {
            body.Append(MakePara("Нет данных.", "Normal", false, "888888", "22"));
        }
        body.Append(new Paragraph());
    }

    private Drawing CreateImageDrawing(string relId)
    {
        var extents = new DW.Extent { Cx = ImgWidth * EmuPerPixel, Cy = ImgHeight * EmuPerPixel };
        var effectExtent = new DW.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L };

        var docProps = new DW.DocProperties { Id = 1U, Name = "Chart" };

        var blipFill = new PIC.BlipFill(
            new D.Blip { Embed = new StringValue(relId) },
            new D.Stretch(new D.FillRectangle())
        );

        var shapeProps = new PIC.ShapeProperties(
            new D.Transform2D(
                new D.Offset { X = 0L, Y = 0L },
                new D.Extents { Cx = ImgWidth * EmuPerPixel, Cy = ImgHeight * EmuPerPixel }
            ),
            new D.PresetGeometry(new D.AdjustValueList()) { Preset = D.ShapeTypeValues.Rectangle }
        );

        var picture = new PIC.Picture(
            new PIC.NonVisualPictureProperties(
                new PIC.NonVisualDrawingProperties { Id = 0U, Name = "Image" },
                new PIC.NonVisualPictureDrawingProperties()
            ),
            blipFill,
            shapeProps
        );

        var graphic = new D.Graphic(
            new D.GraphicData(picture) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }
        );

        var inline = new DW.Inline(extents, effectExtent, docProps, graphic);
        return new Drawing(inline);
    }

    private Styles CreateStyles()
    {
        var styles = new Styles();

        var titleStyle = new Style(new StyleName { Val = "Heading1" })
        {
            Type = StyleValues.Paragraph,
            StyleId = "Heading1",
            Default = true
        };
        titleStyle.Append(new StyleParagraphProperties(
            new SpacingBetweenLines { Before = "240", After = "120" },
            new Justification { Val = JustificationValues.Center }
        ));
        titleStyle.Append(new StyleRunProperties(
            new Bold(),
            new FontSize { Val = "36" },
            new Color { Val = "D44177" },
            new RunFonts { Ascii = "Arial", HighAnsi = "Arial" }
        ));

        var heading2Style = new Style(new StyleName { Val = "Heading2" })
        {
            Type = StyleValues.Paragraph,
            StyleId = "Heading2"
        };
        heading2Style.Append(new StyleParagraphProperties(
            new SpacingBetweenLines { Before = "360", After = "120" },
            new KeepNext(),
            new KeepLines()
        ));
        heading2Style.Append(new StyleRunProperties(
            new Bold(),
            new FontSize { Val = "28" },
            new Color { Val = "D44177" },
            new RunFonts { Ascii = "Arial", HighAnsi = "Arial" }
        ));

        var normalStyle = new Style(new StyleName { Val = "Normal" })
        {
            Type = StyleValues.Paragraph,
            StyleId = "Normal",
            Default = true
        };
        normalStyle.Append(new StyleParagraphProperties(
            new SpacingBetweenLines { Before = "60", After = "60", Line = "276", LineRule = LineSpacingRuleValues.Auto }
        ));
        normalStyle.Append(new StyleRunProperties(
            new FontSize { Val = "22" },
            new Color { Val = "333333" },
            new RunFonts { Ascii = "Arial", HighAnsi = "Arial" }
        ));

        styles.Append(titleStyle);
        styles.Append(heading2Style);
        styles.Append(normalStyle);
        return styles;
    }

    private void AddTitle(Body body)
    {
        body.Append(MakePara("rxxMRKT — Отчёт", "Heading1", true, "D44177", "48"));

        var dateStr = DateTime.UtcNow.ToString("dd.MM.yyyy HH:mm");
        body.Append(new Paragraph(
            new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
            new Run(new RunProperties(new Color { Val = "888888" }, new FontSize { Val = "22" }, new RunFonts { Ascii = "Arial", HighAnsi = "Arial" }),
                new Text($"Сгенерирован: {dateStr}") { Space = SpaceProcessingModeValues.Preserve })
        ));
        body.Append(new Paragraph(new ParagraphProperties(new SpacingBetweenLines { After = "200" })));
    }

    private void AddSalesOverview(Body body, AdminReportViewModel data)
    {
        body.Append(MakePara("1. Обзор продаж", "Heading2", true, "D44177", "28"));

        body.Append(MakeRunText("Всего заказов: ", data.TotalOrders.ToString("N0")));
        body.Append(MakeRunText("Общая выручка: ", data.TotalRevenue.ToString("N0") + " BYN"));
        body.Append(MakeRunText("Средний чек: ", data.AvgOrderValue.ToString("N0") + " BYN"));
        body.Append(MakeRunText("Всего товаров: ", data.TotalProducts.ToString()));
        body.Append(MakeRunText("Всего пользователей: ", data.TotalUsers.ToString()));
        body.Append(MakeRunText("Всего ремонтов: ", data.TotalRepairs.ToString()));
        body.Append(MakeRunText("Средняя стоимость ремонта: ", data.AvgRepairCost.ToString("N0") + " BYN"));
        body.Append(new Paragraph());
    }

    private void AddUnsoldProducts(Body body, AdminReportViewModel data)
    {
        body.Append(MakePara("6. Товары без продаж", "Heading2", true, "D44177", "28"));

        if (data.UnsoldProducts.Count != 0)
        {
            var rows = data.UnsoldProducts.Select(p => new[] { p.Name, p.Price.ToString("N0") + " BYN", p.CreatedAt.ToString("dd.MM.yyyy") }).ToArray();
            body.Append(CreateTable(["Название", "Цена", "Дата добавления"], rows));
        }
        else
        {
            body.Append(MakePara("Все товары имеют продажи.", "Normal", false, "888888", "22"));
        }
        body.Append(new Paragraph());
    }

    private void AddFooter(Body body)
    {
        body.Append(new Paragraph(new ParagraphProperties(
            new SpacingBetweenLines { Before = "600" },
            new Justification { Val = JustificationValues.Center }
        )));
        body.Append(new Paragraph(
            new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
            new Run(new RunProperties(new Color { Val = "AAAAAA" }, new FontSize { Val = "18" }, new Italic(), new RunFonts { Ascii = "Arial", HighAnsi = "Arial" }),
                new Text($"rxxMRKT — Магазин смартфонов | Отчёт сгенерирован {DateTime.UtcNow:dd.MM.yyyy HH:mm}") { Space = SpaceProcessingModeValues.Preserve })
        ));
    }

    private Paragraph MakePara(string text, string styleId, bool bold = false, string color = "333333", string fontSize = "22")
    {
        var runProps = new RunProperties();
        if (bold) runProps.Append(new Bold());
        runProps.Append(new Color { Val = color });
        runProps.Append(new FontSize { Val = fontSize });
        runProps.Append(new RunFonts { Ascii = "Arial", HighAnsi = "Arial" });

        return new Paragraph(
            new ParagraphProperties(new ParagraphStyleId { Val = styleId }),
            new Run(runProps, new Text(text) { Space = SpaceProcessingModeValues.Preserve })
        );
    }

    private Paragraph MakeRunText(string label, string value)
    {
        return new Paragraph(
            new ParagraphProperties(new ParagraphStyleId { Val = "Normal" }),
            new Run(new RunProperties(new Bold(), new FontSize { Val = "22" }, new RunFonts { Ascii = "Arial", HighAnsi = "Arial" }),
                new Text(label) { Space = SpaceProcessingModeValues.Preserve }),
            new Run(new RunProperties(new FontSize { Val = "22" }, new RunFonts { Ascii = "Arial", HighAnsi = "Arial" }),
                new Text(value) { Space = SpaceProcessingModeValues.Preserve })
        );
    }

    private Table CreateTable(string[] headers, string[][] rows)
    {
        var table = new Table();
        var tblPr = new TableProperties(
            new TableStyle { Val = "TableGrid" },
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
            new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4, Color = "D44177" },
                new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "D44177" },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 2, Color = "CCCCCC" },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 2, Color = "CCCCCC" }
            )
        );
        table.Append(tblPr);

        var headerRow = new TableRow();
        foreach (var h in headers)
        {
            var cell = new TableCell(
                new TableCellProperties(
                    new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = "D44177" }
                ),
                new Paragraph(
                    new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                    new Run(new RunProperties(new Bold(), new FontSize { Val = "20" }, new Color { Val = "FFFFFF" }, new RunFonts { Ascii = "Arial", HighAnsi = "Arial" }),
                        new Text(h) { Space = SpaceProcessingModeValues.Preserve })
                )
            );
            headerRow.Append(cell);
        }
        table.Append(headerRow);

        foreach (var rowData in rows)
        {
            var row = new TableRow();
            foreach (var cellText in rowData)
            {
                var cell = new TableCell(
                    new Paragraph(
                        new Run(new RunProperties(new FontSize { Val = "20" }, new RunFonts { Ascii = "Arial", HighAnsi = "Arial" }),
                            new Text(cellText) { Space = SpaceProcessingModeValues.Preserve })
                    )
                );
                row.Append(cell);
            }
            table.Append(row);
        }

        return table;
    }
}
