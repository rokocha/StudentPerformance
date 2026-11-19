using StudentPerformance.Models;

namespace StudentPerformance.Strategies;

/// <summary>Контекст шаблону Strategy: викликає обрану стратегію без прив'язки до її реалізації.</summary>
public class XmlAnalyzer
{
    private IXmlSearchStrategy? _strategy;

    public void SetStrategy(IXmlSearchStrategy strategy) => _strategy = strategy;

    public SearchResult Analyze(string xmlPath, SearchCriteria criteria)
    {
        if (_strategy == null)
            throw new InvalidOperationException("Не обрано спосіб аналізу XML.");

        return _strategy.Search(xmlPath, criteria);
    }
}
