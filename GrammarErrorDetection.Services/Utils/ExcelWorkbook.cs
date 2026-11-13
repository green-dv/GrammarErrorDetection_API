using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;

namespace GrammarErrorDetection.Services.Utils;

internal sealed record ExcelTable(string Name, string Title, string[] Notes, string[] Headers, IEnumerable<object?[]> Rows);

internal static class ExcelWorkbook
{
    private static readonly XNamespace Sheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace Rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace Package = "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace Types = "http://schemas.openxmlformats.org/package/2006/content-types";

    public static byte[] Create(params ExcelTable[] tables)
    {
        using var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            Save(zip, "[Content_Types].xml", new XElement(Types + "Types",
                new XElement(Types + "Default", new XAttribute("Extension", "rels"), new XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml")),
                new XElement(Types + "Default", new XAttribute("Extension", "xml"), new XAttribute("ContentType", "application/xml")),
                new XElement(Types + "Override", new XAttribute("PartName", "/xl/workbook.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml")),
                new XElement(Types + "Override", new XAttribute("PartName", "/xl/styles.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml")),
                Enumerable.Range(1, tables.Length).Select(i => new XElement(Types + "Override", new XAttribute("PartName", $"/xl/worksheets/sheet{i}.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml")))));
            Save(zip, "_rels/.rels", new XElement(Package + "Relationships", Relationship("rId1", "officeDocument", "xl/workbook.xml")));
            Save(zip, "xl/workbook.xml", new XElement(Sheet + "workbook", new XAttribute(XNamespace.Xmlns + "r", Rel),
                new XElement(Sheet + "bookViews", new XElement(Sheet + "workbookView")),
                new XElement(Sheet + "sheets", tables.Select((table, i) => new XElement(Sheet + "sheet", new XAttribute("name", table.Name), new XAttribute("sheetId", i + 1), new XAttribute(Rel + "id", $"rId{i + 1}"))))));
            Save(zip, "xl/_rels/workbook.xml.rels", new XElement(Package + "Relationships",
                Enumerable.Range(1, tables.Length).Select(i => Relationship($"rId{i}", "worksheet", $"worksheets/sheet{i}.xml")),
                Relationship($"rId{tables.Length + 1}", "styles", "styles.xml")));
            Save(zip, "xl/styles.xml", XElement.Parse(Styles));
            for (var i = 0; i < tables.Length; i++)
                WriteSheet(zip, i + 1, tables[i]);
        }
        return output.ToArray();
    }

    private static XElement Relationship(string id, string type, string target) => new(Package + "Relationship",
        new XAttribute("Id", id), new XAttribute("Type", Rel.NamespaceName + "/" + type), new XAttribute("Target", target));

    private static void WriteSheet(ZipArchive zip, int number, ExcelTable table)
    {
        var header = table.Notes.Length + 3;
        var data = table.Rows.ToList();
        var lastColumn = ColumnName(table.Headers.Length);
        var rows = new List<XElement> { Row(1, 30, [Cell(table.Title, 1)]) };
        rows.AddRange(table.Notes.Select((note, i) => Row(i + 2, 22, [Cell(note, 2)])));
        rows.Add(Row(header, 34, table.Headers.Select(label => Cell(label, 3))));
        rows.AddRange(data.Select((values, i) => Row(header + i + 1, 23,
            values.Select(value => Cell(value, i % 2 == 0 ? 4 : 5)))));

        var widths = table.Headers.Select((label, i) =>
        {
            var longest = data.Select(row => row[i]?.ToString()?.Length ?? 0).DefaultIfEmpty().Max();
            return new XElement(Sheet + "col", new XAttribute("min", i + 1), new XAttribute("max", i + 1),
                new XAttribute("width", Math.Clamp(Math.Max(label.Length, longest) + 4, 14, 42)), new XAttribute("customWidth", 1));
        });

        Save(zip, $"xl/worksheets/sheet{number}.xml", new XElement(Sheet + "worksheet",
            new XElement(Sheet + "sheetViews", new XElement(Sheet + "sheetView", new XAttribute("workbookViewId", 0),
                new XAttribute("showGridLines", 0),
                new XElement(Sheet + "pane", new XAttribute("ySplit", header), new XAttribute("topLeftCell", $"A{header + 1}"),
                    new XAttribute("activePane", "bottomLeft"), new XAttribute("state", "frozen")))),
            new XElement(Sheet + "cols", widths),
            new XElement(Sheet + "sheetData", rows),
            new XElement(Sheet + "autoFilter", new XAttribute("ref", $"A{header}:{lastColumn}{header + data.Count}")),
            new XElement(Sheet + "mergeCells", Enumerable.Range(1, table.Notes.Length + 1)
                .Select(i => new XElement(Sheet + "mergeCell", new XAttribute("ref", $"A{i}:{lastColumn}{i}"))))));
    }

    private static XElement Row(int number, int height, IEnumerable<XElement> cells) =>
        new(Sheet + "row", new XAttribute("r", number), new XAttribute("ht", height),
            new XAttribute("customHeight", 1), cells);

    private static string ColumnName(int index)
    {
        var name = string.Empty;
        while (index > 0)
        {
            index--;
            name = (char)('A' + index % 26) + name;
            index /= 26;
        }
        return name;
    }

    private static XElement Cell(object? value, int style)
    {
        var cell = new XElement(Sheet + "c", new XAttribute("s", style));
        if (value is null) return cell;
        if (value is decimal proportion)
        {
            cell.Add(new XElement(Sheet + "v",
                decimal.Round(proportion, 2, MidpointRounding.AwayFromZero).ToString("0.##", CultureInfo.InvariantCulture)));
            return cell;
        }
        if (value is long or int)
        {
            cell.Add(new XElement(Sheet + "v", Convert.ToString(value, CultureInfo.InvariantCulture)));
            return cell;
        }
        cell.Add(new XAttribute("t", "inlineStr"),
            new XElement(Sheet + "is", new XElement(Sheet + "t", new XAttribute(XNamespace.Xml + "space", "preserve"), value.ToString())));
        return cell;
    }

    private const string Styles = """
        <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
          <fonts count="3">
            <font><sz val="11"/><color rgb="FF16263A"/><name val="Calibri"/></font>
            <font><b/><sz val="17"/><color rgb="FFFFFFFF"/><name val="Calibri"/></font>
            <font><b/><sz val="11"/><color rgb="FFFFFFFF"/><name val="Calibri"/></font>
          </fonts>
          <fills count="6">
            <fill><patternFill patternType="none"/></fill>
            <fill><patternFill patternType="gray125"/></fill>
            <fill><patternFill patternType="solid"><fgColor rgb="FF17365D"/><bgColor indexed="64"/></patternFill></fill>
            <fill><patternFill patternType="solid"><fgColor rgb="FFE5EFF8"/><bgColor indexed="64"/></patternFill></fill>
            <fill><patternFill patternType="solid"><fgColor rgb="FF21666B"/><bgColor indexed="64"/></patternFill></fill>
            <fill><patternFill patternType="solid"><fgColor rgb="FFF1F6FA"/><bgColor indexed="64"/></patternFill></fill>
          </fills>
          <borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders>
          <cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>
          <cellXfs count="6">
            <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>
            <xf numFmtId="0" fontId="1" fillId="2" borderId="0" xfId="0" applyFont="1" applyFill="1" applyAlignment="1"><alignment vertical="center" indent="1"/></xf>
            <xf numFmtId="0" fontId="0" fillId="3" borderId="0" xfId="0" applyFill="1" applyAlignment="1"><alignment vertical="center" indent="1"/></xf>
            <xf numFmtId="0" fontId="2" fillId="4" borderId="0" xfId="0" applyFont="1" applyFill="1" applyAlignment="1"><alignment vertical="center" wrapText="1"/></xf>
            <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0" applyAlignment="1"><alignment vertical="center"/></xf>
            <xf numFmtId="0" fontId="0" fillId="5" borderId="0" xfId="0" applyFill="1" applyAlignment="1"><alignment vertical="center"/></xf>
          </cellXfs>
          <cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles>
        </styleSheet>
        """;

    private static void Save(ZipArchive zip, string path, XElement xml)
    {
        using var stream = zip.CreateEntry(path, CompressionLevel.Fastest).Open();
        new XDocument(new XDeclaration("1.0", "utf-8", null), xml).Save(stream);
    }
}
