using StudentPerformance.Models;

namespace StudentPerformance.Strategies;

/// <summary>
/// Шаблон проєктування "Стратегія" (Strategy): спільний інтерфейс для трьох способів аналізу XML.
/// </summary>
public interface IXmlSearchStrategy
{
    string Name { get; }
    SearchResult Search(string xmlPath, SearchCriteria criteria);
}
