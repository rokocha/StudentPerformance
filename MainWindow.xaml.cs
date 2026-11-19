using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using StudentPerformance.Models;
using StudentPerformance.Services;
using StudentPerformance.Strategies;

namespace StudentPerformance;

public partial class MainWindow : Window
{
    private const string AnyValue = "— будь-яке —";

    // Контекст та три реалізації шаблону Strategy
    private readonly XmlAnalyzer _analyzer = new();
    private readonly List<IXmlSearchStrategy> _strategies = new()
    {
        new SaxSearchStrategy(),
        new DomSearchStrategy(),
        new LinqSearchStrategy()
    };

    // Динамічно згенеровані ComboBox-и для значень атрибутів, підвантажених із файлу
    private readonly Dictionary<string, ComboBox> _studentCombos = new();
    private readonly Dictionary<string, ComboBox> _disciplineCombos = new();

    private string? _xmlPath;
    private SearchResult? _lastResult;
    private bool _exitConfirmed;

    public MainWindow()
    {
        InitializeComponent();

        StrategyComboBox.ItemsSource = _strategies.Select(s => s.Name).ToList();
        StrategyComboBox.SelectedIndex = 0;

        // Автоматично підвантажуємо приклад, якщо він поруч із програмою
        string samplePath = Path.Combine(AppContext.BaseDirectory, "Data", "students.xml");
        if (File.Exists(samplePath))
            LoadXmlFile(samplePath);
    }

    // ───────────────────────── 1. Вибір та підгрузка XML ─────────────────────────

    private void OnPickFileClicked(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Оберіть XML-файл",
            Filter = "XML файли (*.xml)|*.xml|Усі файли (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) == true)
            LoadXmlFile(dialog.FileName);
    }

    private void OnLoadSampleClicked(object sender, RoutedEventArgs e)
    {
        string samplePath = Path.Combine(AppContext.BaseDirectory, "Data", "students.xml");
        if (!File.Exists(samplePath))
        {
            MessageBox.Show(this, $"Файл прикладу не знайдено:\n{samplePath}",
                "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        LoadXmlFile(samplePath);
    }

    /// <summary>"Підгрузка даних": сканує атрибути та значення, реально присутні у файлі.</summary>
    private void LoadXmlFile(string path)
    {
        try
        {
            XmlMetadata meta = XmlMetadataScanner.Scan(path);
            _xmlPath = path;
            _lastResult = null;

            FilePathTextBox.Text = path;
            FileInfoText.Text = $"Студентів у файлі: {meta.StudentCount}  •  " +
                                $"Атрибутів студента: {meta.StudentAttributes.Count}  •  " +
                                $"Атрибутів дисципліни: {meta.DisciplineAttributes.Count}";

            BuildAttributeControls(StudentAttrsPanel, meta.StudentAttributes, _studentCombos);
            BuildAttributeControls(DisciplineAttrsPanel, meta.DisciplineAttributes, _disciplineCombos);
            OutputTextBox.Clear();
        }
        catch (Exception ex)
        {
            _xmlPath = null;
            MessageBox.Show(this, $"Не вдалося прочитати XML-файл:\n{ex.Message}",
                "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static void BuildAttributeControls(
        StackPanel panel,
        Dictionary<string, HashSet<string>> attributes,
        Dictionary<string, ComboBox> store)
    {
        panel.Children.Clear();
        store.Clear();

        foreach (var (name, values) in attributes)
        {
            var items = new List<string> { AnyValue };
            items.AddRange(SortValues(values));

            var combo = new ComboBox
            {
                ItemsSource = items,
                SelectedIndex = 0
            };

            var row = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(135) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var label = new TextBlock
            {
                Text = AttributeLabels.Get(name),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(label, 0);
            Grid.SetColumn(combo, 1);

            row.Children.Add(label);
            row.Children.Add(combo);

            panel.Children.Add(row);
            store[name] = combo;
        }
    }

    private static List<string> SortValues(IEnumerable<string> values)
    {
        var list = values.ToList();
        if (list.All(v => double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out _)))
            return list.OrderBy(v => double.Parse(v, CultureInfo.InvariantCulture)).ToList();

        var comparer = StringComparer.Create(new CultureInfo("uk-UA"), true);
        return list.OrderBy(v => v, comparer).ToList();
    }

    // ───────────────────────── 2. Пошук через Strategy ─────────────────────────

    private SearchCriteria ReadCriteria()
    {
        var criteria = new SearchCriteria { Keyword = KeywordTextBox.Text.Trim() };

        foreach (var (name, combo) in _studentCombos)
            if (combo.SelectedIndex > 0 && combo.SelectedItem is string val)
                criteria.StudentAttributes[name] = val;

        foreach (var (name, combo) in _disciplineCombos)
            if (combo.SelectedIndex > 0 && combo.SelectedItem is string val)
                criteria.DisciplineAttributes[name] = val;

        return criteria;
    }

    private void OnSearchClicked(object sender, RoutedEventArgs e)
    {
        if (_xmlPath == null)
        {
            MessageBox.Show(this, "Спочатку оберіть XML-файл.",
                "Увага", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        SearchCriteria criteria = ReadCriteria();
        if (criteria.IsEmpty)
        {
            MessageBox.Show(this, "Оберіть значення хоча б одного атрибута або введіть ключове слово.",
                "Увага", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            _analyzer.SetStrategy(_strategies[StrategyComboBox.SelectedIndex]);

            var sw = Stopwatch.StartNew();
            SearchResult result = _analyzer.Analyze(_xmlPath, criteria);
            sw.Stop();

            _lastResult = result;
            OutputTextBox.Text = ResultFormatter.ToText(result, sw.Elapsed);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Помилка під час аналізу:\n{ex.Message}",
                "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ───────────────────────── 3. Трансформація у HTML (XSLT) ─────────────────────────

    private void OnTransformHtmlClicked(object sender, RoutedEventArgs e)
    {
        if (_xmlPath == null)
        {
            MessageBox.Show(this, "Спочатку оберіть XML-файл.",
                "Увага", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        string xslPath = Path.Combine(AppContext.BaseDirectory, "Data", "students.xsl");
        if (!File.Exists(xslPath))
        {
            MessageBox.Show(this, $"XSL-файл не знайдено:\n{xslPath}",
                "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var saveDialog = new SaveFileDialog
        {
            Title = "Зберегти HTML-файл",
            Filter = "HTML файли (*.html)|*.html",
            FileName = "students_report.html"
        };

        if (saveDialog.ShowDialog(this) != true) return;

        try
        {
            string sourceXml = _xmlPath;
            string? tempXml = null;

            // Якщо пошук уже виконувався — трансформуємо результати пошуку, інакше весь XML-документ
            if (_lastResult != null)
            {
                tempXml = Path.Combine(Path.GetTempPath(), $"student_result_{Guid.NewGuid():N}.xml");
                XsltTransformService.SaveResultXml(_lastResult, tempXml);
                sourceXml = tempXml;
            }

            XsltTransformService.TransformToHtml(sourceXml, xslPath, saveDialog.FileName);

            if (tempXml != null && File.Exists(tempXml))
                File.Delete(tempXml);

            Process.Start(new ProcessStartInfo(saveDialog.FileName) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Помилка трансформації в HTML:\n{ex.Message}",
                "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ───────────────────────── 4. Очищення та підтвердження виходу ─────────────────────────

    private void OnClearClicked(object sender, RoutedEventArgs e)
    {
        KeywordTextBox.Clear();
        foreach (var combo in _studentCombos.Values) combo.SelectedIndex = 0;
        foreach (var combo in _disciplineCombos.Values) combo.SelectedIndex = 0;
        OutputTextBox.Clear();
        _lastResult = null;
    }

    private void OnExitClicked(object sender, RoutedEventArgs e) => Close();

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (_exitConfirmed) return;

        if (ShowExitConfirmation())
            _exitConfirmed = true;
        else
            e.Cancel = true;
    }

    /// <summary>
    /// Діалог виходу в темному стилі з точним текстом та кнопками «Так» і «Ні».
    /// </summary>
    private bool ShowExitConfirmation()
    {
        bool confirmed = false;

        var dialog = new Window
        {
            Title = "Підтвердження виходу",
            Background = (Brush)Application.Current.Resources["BgMainBrush"],
            Width = 410,
            Height = 160,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this
        };

        var panel = new StackPanel { Margin = new Thickness(20) };
        panel.Children.Add(new TextBlock
        {
            Text = "Чи дійсно ви хочете завершити роботу з програмою?",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 18)
        });

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var yesButton = new Button { Content = "Так", Width = 90, Margin = new Thickness(0, 0, 10, 0) };
        var noButton = new Button { Content = "Ні", Width = 90, IsCancel = true };

        yesButton.Click += (_, _) => { confirmed = true; dialog.Close(); };
        noButton.Click += (_, _) => dialog.Close();

        buttons.Children.Add(yesButton);
        buttons.Children.Add(noButton);
        panel.Children.Add(buttons);

        dialog.Content = panel;
        dialog.ShowDialog();

        return confirmed;
    }
}
