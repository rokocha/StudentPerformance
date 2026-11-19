using System.Xml;
using StudentPerformance.Models;
using StudentPerformance.Services;

namespace StudentPerformance.Strategies;

/// <summary>
/// SAX API: послідовне потокове читання XML через XmlReader без завантаження дерева в пам'ять.
/// </summary>
public class SaxSearchStrategy : IXmlSearchStrategy
{
    public string Name => "SAX API (XmlReader)";

    public SearchResult Search(string xmlPath, SearchCriteria criteria)
    {
        var predicates = BuildPredicates(criteria, out string description);
        var result = new SearchResult { StrategyName = Name, Query = description };

        var settings = new XmlReaderSettings
        {
            IgnoreComments = true,
            IgnoreWhitespace = true,
            DtdProcessing = DtdProcessing.Prohibit
        };

        using var reader = XmlReader.Create(xmlPath, settings);
        StudentRecord? current = null;

        while (reader.Read())
        {
            switch (reader.NodeType)
            {
                case XmlNodeType.Element when reader.Name == "student":
                    bool isEmpty = reader.IsEmptyElement;
                    current = new StudentRecord();
                    reader.ReadAttributesInto(current.Attributes);
                    if (isEmpty) FinishStudent(ref current, predicates, result);
                    break;

                case XmlNodeType.Element when reader.Name == "discipline" && current != null:
                    var discipline = new DisciplineRecord();
                    reader.ReadAttributesInto(discipline.Attributes);
                    current.Disciplines.Add(discipline);
                    break;

                case XmlNodeType.EndElement when reader.Name == "student" && current != null:
                    FinishStudent(ref current, predicates, result);
                    break;
            }
        }

        return result;
    }

    private static void FinishStudent(
        ref StudentRecord? current,
        List<Func<StudentRecord, bool>> predicates,
        SearchResult result)
    {
        var student = current;
        if (student != null && predicates.All(p => p(student)))
            result.Students.Add(student);
        current = null;
    }

    private static List<Func<StudentRecord, bool>> BuildPredicates(SearchCriteria c, out string description)
    {
        var predicates = new List<Func<StudentRecord, bool>>();
        var parts = new List<string>();

        foreach (var (attr, value) in c.StudentAttributes)
        {
            string a = attr, v = value;
            predicates.Add(s => s.Attributes.TryGetValue(a, out var actual) && actual == v);
            parts.Add($"student.{a} == \"{v}\"");
        }

        if (c.DisciplineAttributes.Count > 0)
        {
            var conditions = c.DisciplineAttributes.ToDictionary(kv => kv.Key, kv => kv.Value);
            predicates.Add(s => s.Disciplines.Any(d => conditions.All(kv =>
                d.Attributes.TryGetValue(kv.Key, out var actual) && actual == kv.Value)));
            parts.Add("discipline(" + string.Join(" && ", conditions.Select(kv => $"{kv.Key} == \"{kv.Value}\"")) + ")");
        }

        if (!string.IsNullOrWhiteSpace(c.Keyword))
        {
            string kw = c.Keyword.Trim();
            predicates.Add(s =>
                s.Attributes.Values.Any(v => v.Contains(kw, StringComparison.OrdinalIgnoreCase)) ||
                s.Disciplines.Any(d => d.Attributes.Values.Any(v => v.Contains(kw, StringComparison.OrdinalIgnoreCase))));
            parts.Add($"keyword contains \"{kw}\"");
        }

        description = parts.Count > 0 ? string.Join(" AND ", parts) : "усі студенти";
        return predicates;
    }
}
