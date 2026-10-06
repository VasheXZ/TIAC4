using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TIAC4;

// Главное окно приложения
public partial class MainWindow : Window
{
    private static readonly string[] DefaultProbabilities =
        ["0,400", "0,150", "0,150", "0,100", "0,100", "0,080", "0,020"];

    private HuffmanResult? _currentResult;

    // Инициализация главного окна
    public MainWindow()
    {
        InitializeComponent();
    }

    private (string Symbol, TextBox Input)[] ProbabilityInputs =>
    [
        ("S1", ProbabilityS1TextBox),
        ("S2", ProbabilityS2TextBox),
        ("S3", ProbabilityS3TextBox),
        ("S4", ProbabilityS4TextBox),
        ("S5", ProbabilityS5TextBox),
        ("S6", ProbabilityS6TextBox),
        ("S7", ProbabilityS7TextBox)
    ];

    // Обработка построения кода
    private void BuildButton_Click(object sender, RoutedEventArgs e) =>
        TryBuild();

    // Обработка кодирования сообщения
    private void EncodeButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            EncodedTextBox.Text = HuffmanCodec.Encode(SourceMessageTextBox.Text, CurrentResult);
            DecodedMessageTextBox.Clear();
            VerificationTextBlock.Text = string.Empty;
            StatusTextBlock.Text = "Сообщение закодировано.";
        }
        catch (Exception exception)
        {
            ShowError(exception.Message);
        }
    }

    // Обработка декодирования сообщения
    private void DecodeButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string decoded = HuffmanCodec.Decode(EncodedTextBox.Text, CurrentResult);
            DecodedMessageTextBox.Text = decoded;

            string normalizedSource = NormalizeMessage(SourceMessageTextBox.Text);
            bool matches = string.Equals(decoded, normalizedSource, StringComparison.OrdinalIgnoreCase);
            VerificationTextBlock.Text = matches
                ? "Проверка пройдена: декодированное сообщение совпадает с исходным."
                : "Декодирование выполнено. Результат отличается от исходного сообщения.";
            VerificationTextBlock.Foreground = matches ? Brushes.DarkGreen : Brushes.DarkOrange;
            StatusTextBlock.Text = "Битовая последовательность декодирована.";
        }
        catch (Exception exception)
        {
            ShowError(exception.Message);
        }
    }

    // Обработка сброса данных
    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        for (int index = 0; index < ProbabilityInputs.Length; index++)
        {
            ProbabilityInputs[index].Input.Text = DefaultProbabilities[index];
        }

        SourceMessageTextBox.Text = "S1 S2 S6 S1 S7";
        ClearResults();
        StatusTextBlock.Text = string.Empty;
        _currentResult = null;
        EncodeButton.IsEnabled = DecodeButton.IsEnabled = false;
    }

    // Изменение вероятности
    private void ProbabilityTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsInitialized)
        {
            return;
        }

        InvalidateCurrentCode();
    }

    // Построение кода с обработкой ошибок
    private void TryBuild()
    {
        try
        {
            var symbols = ProbabilityInputs
                .Select(item => new SymbolProbability(
                    item.Symbol,
                    ParseProbability(item.Input.Text, item.Symbol)))
                .ToArray();

            _currentResult = HuffmanCodec.Build(symbols);
            ResultTextBox.Text = _currentResult.ToString();
            StatusTextBlock.Foreground = Brushes.DarkGreen;
            StatusTextBlock.Text = "Код Хаффмана построен.";
            EncodeButton.IsEnabled = DecodeButton.IsEnabled = true;
        }
        catch (Exception exception)
        {
            _currentResult = null;
            StatusTextBlock.Foreground = Brushes.DarkRed;
            StatusTextBlock.Text = "Исправьте исходные данные.";
            EncodeButton.IsEnabled = DecodeButton.IsEnabled = false;
            ShowError(exception.Message);
        }
    }

    // Сброс построенного кода
    private void InvalidateCurrentCode()
    {
        _currentResult = null;
        ClearResults();
        StatusTextBlock.Foreground = Brushes.DarkOrange;
        StatusTextBlock.Text = "Требуется повторное построение кода.";
        EncodeButton.IsEnabled = DecodeButton.IsEnabled = false;
    }

    // Текущий результат построения
    private HuffmanResult CurrentResult => _currentResult
        ?? throw new InvalidOperationException("Сначала постройте код Хаффмана.");

    // Очистка результатов
    private void ClearResults()
    {
        ResultTextBox.Clear();
        EncodedTextBox.Clear();
        DecodedMessageTextBox.Clear();
        VerificationTextBlock.Text = string.Empty;
    }

    // Преобразование текста в вероятность
    private static decimal ParseProbability(string text, string symbol)
    {
        string normalized = text.Trim().Replace(',', '.');
        if (!decimal.TryParse(
                normalized,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out decimal probability))
        {
            throw new ArgumentException($"Для {symbol} указана некорректная вероятность.");
        }

        return probability;
    }

    // Нормализация исходного сообщения
    private static string NormalizeMessage(string message) =>
        string.Join(
            ' ',
            message
                .Split([' ', '\t', '\r', '\n', ',', ';'], StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.ToUpperInvariant()));

    // Сообщение об ошибке
    private static void ShowError(string message)
    {
        MessageBox.Show(
            message,
            "Ошибка",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
