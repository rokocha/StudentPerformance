namespace StudentPerformance.Models;

/// <summary>Українські підписи атрибутів та фільтр службових атрибутів.</summary>
public static class AttributeLabels
{
    private static readonly Dictionary<string, string> Labels = new()
    {
        ["ID"] = "Код",
        ["PIB"] = "П.І.П.",
        ["faculty"] = "Факультет",
        ["department"] = "Кафедра",
        ["course"] = "Курс",
        ["group"] = "Група",
        ["form"] = "Форма навчання",
        ["scholarship"] = "Стипендія",
        ["email"] = "E-mail",
        ["phone"] = "Телефон",
        ["name"] = "Дисципліна",
        ["semester"] = "Семестр",
        ["credits"] = "Кредити",
        ["control"] = "Контроль",
        ["teacher"] = "Викладач",
        ["grade"] = "Оцінка",
    };

    public static string Get(string name) => Labels.TryGetValue(name, out var label) ? label : name;

    /// <summary>Атрибути просторів імен (xmlns:*, xml:lang) не належать до даних.</summary>
    public static bool IsDataAttribute(string name) =>
        !name.Contains(':') && !name.Equals("xmlns", StringComparison.Ordinal);
}

/// <summary>Запис про навчальну дисципліну (вузол &lt;discipline&gt;).</summary>
public class DisciplineRecord
{
    public Dictionary<string, string> Attributes { get; } = new();
}

/// <summary>Запис про студента (головний вузол &lt;student&gt;, по якому здійснюється пошук).</summary>
public class StudentRecord
{
    public Dictionary<string, string> Attributes { get; } = new();
    public List<DisciplineRecord> Disciplines { get; } = new();

    public string Pib => Attributes.TryGetValue("PIB", out var value) ? value : "(без П.І.П.)";
}

/// <summary>Параметри пошуку, які задає користувач.</summary>
public class SearchCriteria
{
    public Dictionary<string, string> StudentAttributes { get; } = new();
    public Dictionary<string, string> DisciplineAttributes { get; } = new();
    public string Keyword { get; set; } = "";

    public bool IsEmpty =>
        StudentAttributes.Count == 0 &&
        DisciplineAttributes.Count == 0 &&
        string.IsNullOrWhiteSpace(Keyword);
}

/// <summary>Результат роботи обраної стратегії аналізу XML.</summary>
public class SearchResult
{
    public string StrategyName { get; set; } = "";
    public string Query { get; set; } = "";
    public List<StudentRecord> Students { get; } = new();
}

/// <summary>Атрибути та їх реальні значення, підвантажені з обраного XML-файлу.</summary>
public class XmlMetadata
{
    public int StudentCount { get; set; }
    public Dictionary<string, HashSet<string>> StudentAttributes { get; } = new();
    public Dictionary<string, HashSet<string>> DisciplineAttributes { get; } = new();
}
