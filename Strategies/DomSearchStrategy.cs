using System.Text;
using System.Xml;
using StudentPerformance.Models;

namespace StudentPerformance.Strategies;

/// <summary>
/// DOM API: завантаження XML у пам'ять (XmlDocument) та пошук динамічно згенерованим XPath-запитом.
/// </summary>
public class DomSearchStrategy : IXmlSearchStrategy
{
    public string Name => "DOM API (XmlDocument + XPath)";

    private const string Upper = "АБВГҐДЕЄЖЗИІЇЙКЛМНОПРСТУФХЦЧШЩЬЮЯЁЫЭЪABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Lower = "абвгґдеєжзиіїйклмнопрстуфхцчшщьюяёыэъabcdefghijklmnopqrstuvwxyz";

    public SearchResult Search(string xmlPath, SearchCriteria criteria)
    {
        string xpath = BuildXPath(criteria);
        var result = new SearchResult { StrategyName = Name, Query = xpath };

        var doc = new XmlDocument();
        doc.Load(xmlPath);

        XmlNodeList? nodes = doc.SelectNodes(xpath);
        if (nodes == null) return result;

        foreach (XmlNode node in nodes)
        {
            var student = new StudentRecord();
            if (node.Attributes != null)
            {
                foreach (XmlAttribute attr in node.Attributes)
                    if (AttributeLabels.IsDataAttribute(attr.Name))
                        student.Attributes[attr.Name] = attr.Value;
            }

            XmlNodeList? disciplines = node.SelectNodes("discipline");
            if (disciplines != null)
            {
                foreach (XmlNode d in disciplines)
                {
                    var discipline = new DisciplineRecord();
                    if (d.Attributes != null)
                    {
                        foreach (XmlAttribute attr in d.Attributes)
                            if (AttributeLabels.IsDataAttribute(attr.Name))
                                discipline.Attributes[attr.Name] = attr.Value;
                    }
                    student.Disciplines.Add(discipline);
                }
            }

            result.Students.Add(student);
        }

        return result;
    }

    public static string BuildXPath(SearchCriteria c)
    {
        var sb = new StringBuilder("//student");

        if (c.StudentAttributes.Count > 0)
        {
            sb.Append('[')
              .Append(string.Join(" and ", c.StudentAttributes.Select(kv => $"@{kv.Key}={Literal(kv.Value)}")))
              .Append(']');
        }

        if (c.DisciplineAttributes.Count > 0)
        {
            sb.Append("[discipline[")
              .Append(string.Join(" and ", c.DisciplineAttributes.Select(kv => $"@{kv.Key}={Literal(kv.Value)}")))
              .Append("]]");
        }

        if (!string.IsNullOrWhiteSpace(c.Keyword))
        {
            string kw = c.Keyword.Trim().ToLowerInvariant();
            sb.Append($"[.//@*[contains(translate(., '{Upper}', '{Lower}'), {Literal(kw)})]]");
        }

        return sb.ToString();
    }

    private static string Literal(string value)
    {
        if (!value.Contains('\'')) return $"'{value}'";
        if (!value.Contains('"')) return $"\"{value}\"";
        return "concat(" + string.Join(", \"'\", ", value.Split('\'').Select(p => $"'{p}'")) + ")";
    }
}
