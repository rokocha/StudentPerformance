using System.Xml.Linq;
using StudentPerformance.Models;

namespace StudentPerformance.Strategies;

/// <summary>
/// LINQ to XML: завантаження XML в XDocument та динамічна побудова ланцюжка Where-запитів.
/// </summary>
public class LinqSearchStrategy : IXmlSearchStrategy
{
    public string Name => "LINQ to XML (XDocument)";

    public SearchResult Search(string xmlPath, SearchCriteria criteria)
    {
        var parts = new List<string> { "doc.Descendants(\"student\")" };
        var doc = XDocument.Load(xmlPath);

        IEnumerable<XElement> query = doc.Descendants("student");

        foreach (var (attr, value) in criteria.StudentAttributes)
        {
            string a = attr, v = value;
            query = query.Where(s => (string?)s.Attribute(a) == v);
            parts.Add($".Where(s => (string)s.Attribute(\"{a}\") == \"{v}\")");
        }

        if (criteria.DisciplineAttributes.Count > 0)
        {
            var conditions = criteria.DisciplineAttributes.ToDictionary(kv => kv.Key, kv => kv.Value);
            query = query.Where(s => s.Elements("discipline")
                .Any(d => conditions.All(kv => (string?)d.Attribute(kv.Key) == kv.Value)));
            parts.Add(".Where(s => s.Elements(\"discipline\").Any(d => " +
                      string.Join(" && ", conditions.Select(kv => $"(string)d.Attribute(\"{kv.Key}\") == \"{kv.Value}\"")) + "))");
        }

        if (!string.IsNullOrWhiteSpace(criteria.Keyword))
        {
            string kw = criteria.Keyword.Trim();
            query = query.Where(s => s.DescendantsAndSelf().Attributes()
                .Any(at => at.Value.Contains(kw, StringComparison.OrdinalIgnoreCase)));
            parts.Add($".Where(s => s.DescendantsAndSelf().Attributes().Any(at => at.Value.Contains(\"{kw}\")))");
        }

        var result = new SearchResult { StrategyName = Name, Query = string.Concat(parts) };

        foreach (XElement element in query)
        {
            var student = new StudentRecord();
            foreach (XAttribute attr in element.Attributes())
                if (AttributeLabels.IsDataAttribute(attr.Name.LocalName))
                    student.Attributes[attr.Name.LocalName] = attr.Value;

            foreach (XElement d in element.Elements("discipline"))
            {
                var discipline = new DisciplineRecord();
                foreach (XAttribute attr in d.Attributes())
                    if (AttributeLabels.IsDataAttribute(attr.Name.LocalName))
                        discipline.Attributes[attr.Name.LocalName] = attr.Value;
                student.Disciplines.Add(discipline);
            }

            result.Students.Add(student);
        }

        return result;
    }
}
