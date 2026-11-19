using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;
using StudentPerformance.Models;

namespace StudentPerformance.Services;

/// <summary>Допоміжне розширення для читання атрибутів через XmlReader.</summary>
public static class XmlReaderExtensions
{
    public static void ReadAttributesInto(this XmlReader reader, IDictionary<string, string> target)
    {
        if (!reader.HasAttributes) return;
        while (reader.MoveToNextAttribute())
        {
            if (AttributeLabels.IsDataAttribute(reader.Name))
                target[reader.Name] = reader.Value;
        }
        reader.MoveToElement();
    }
}

/// <summary>"Підгрузка даних": зчитує з XML-файлу всі наявні атрибути та їх унікальні значення.</summary>
public static class XmlMetadataScanner
{
    public static XmlMetadata Scan(string path)
    {
        var meta = new XmlMetadata();
        var settings = new XmlReaderSettings
        {
            IgnoreComments = true,
            IgnoreWhitespace = true,
            DtdProcessing = DtdProcessing.Prohibit
        };

        using var reader = XmlReader.Create(path, settings);
        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element) continue;

            if (reader.Name == "student")
            {
                meta.StudentCount++;
                CollectAttributes(reader, meta.StudentAttributes);
            }
            else if (reader.Name == "discipline")
            {
                CollectAttributes(reader, meta.DisciplineAttributes);
            }
        }

        if (meta.StudentCount == 0)
            throw new InvalidDataException("У файлі не знайдено жодного елемента <student>.");

        return meta;
    }

    private static void CollectAttributes(XmlReader reader, Dictionary<string, HashSet<string>> target)
    {
        var attrs = new Dictionary<string, string>();
        reader.ReadAttributesInto(attrs);
        foreach (var (name, value) in attrs)
        {
            if (!target.TryGetValue(name, out var set))
                target[name] = set = new HashSet<string>(StringComparer.Ordinal);
            set.Add(value);
        }
    }
}

/// <summary>Форматування результатів пошуку у текст для поля виводу.</summary>
public static class ResultFormatter
{
    public static string ToText(SearchResult r, TimeSpan elapsed)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Спосіб аналізу: {r.StrategyName}");
        sb.AppendLine($"Згенерований запит: {r.Query}");
        sb.AppendLine($"Знайдено студентів: {r.Students.Count}   (час: {elapsed.TotalMilliseconds:F1} мс)");
        sb.AppendLine(new string('─', 56));

        int index = 1;
        foreach (var s in r.Students)
        {
            sb.AppendLine($"{index++}. {s.Pib}");
            foreach (var (k, v) in s.Attributes)
                if (k != "PIB") sb.AppendLine($"     {AttributeLabels.Get(k)}: {v}");

            if (s.Disciplines.Count > 0)
            {
                sb.AppendLine("     Дисципліни:");
                foreach (var d in s.Disciplines)
                {
                    string name = d.Attributes.TryGetValue("name", out var n) ? n : "(без назви)";
                    string details = string.Join(", ", d.Attributes
                        .Where(kv => kv.Key != "name")
                        .Select(kv => $"{AttributeLabels.Get(kv.Key)}: {kv.Value}"));
                    sb.AppendLine($"       • {name}" + (details.Length > 0 ? $" — {details}" : ""));
                }

                var grades = s.Disciplines
                    .Select(d => d.Attributes.TryGetValue("grade", out var g) &&
                                 double.TryParse(g, NumberStyles.Float, CultureInfo.InvariantCulture, out var val)
                        ? val
                        : (double?)null)
                    .Where(x => x.HasValue)
                    .Select(x => x!.Value)
                    .ToList();

                if (grades.Count > 0)
                    sb.AppendLine($"     Середній бал: {grades.Average():F1}");
            }
            sb.AppendLine();
        }

        if (r.Students.Count == 0)
            sb.AppendLine("За заданими параметрами нічого не знайдено.");

        return sb.ToString();
    }
}

/// <summary>Збереження результатів пошуку в XML та XSLT-трансформація в HTML.</summary>
public static class XsltTransformService
{
    public static void SaveResultXml(SearchResult result, string xmlOutputPath)
    {
        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("university",
                new XAttribute("strategy", result.StrategyName),
                new XAttribute("query", result.Query),
                new XAttribute("count", result.Students.Count),
                result.Students.Select(s =>
                    new XElement("student",
                        s.Attributes.Select(kv => new XAttribute(kv.Key, kv.Value)),
                        s.Disciplines.Select(d =>
                            new XElement("discipline",
                                d.Attributes.Select(kv => new XAttribute(kv.Key, kv.Value))))))));
        doc.Save(xmlOutputPath);
    }

    public static void TransformToHtml(string xmlPath, string xslPath, string htmlPath)
    {
        var xslt = new XslCompiledTransform();
        xslt.Load(xslPath);
        xslt.Transform(xmlPath, htmlPath);
    }
}
