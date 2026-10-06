using System.Globalization;
using System.Text;

namespace TIAC4;

// Вероятность символа
public sealed record SymbolProbability(string Symbol, decimal Probability);

// Узел дерева Хаффмана
public sealed record HuffmanNode(
    decimal Probability,
    int CreationOrder,
    string Label,
    string? Symbol = null,
    HuffmanNode? Left = null,
    HuffmanNode? Right = null)
{
    public bool IsLeaf => Symbol is not null;
}

// Объединение узлов дерева
public sealed record HuffmanMerge(
    int Step,
    HuffmanNode First,
    HuffmanNode Second,
    HuffmanNode Result);

// Результат построения кода Хаффмана
public sealed record HuffmanResult(
    IReadOnlyList<SymbolProbability> Symbols,
    IReadOnlyList<IReadOnlyList<decimal>> MergeSteps,
    IReadOnlyList<HuffmanMerge> Merges,
    IReadOnlyDictionary<string, string> Codes,
    HuffmanNode Root,
    double AverageCodeLength)
{
    // Текстовое представление результата
    public override string ToString()
    {
        var text = new StringBuilder();

        text.AppendLine("ИСХОДНЫЕ ВЕРОЯТНОСТИ");
        foreach (SymbolProbability item in Symbols)
        {
            text.AppendLine($"{item.Symbol}: {FormatProbability(item.Probability)}");
        }

        text.AppendLine();
        text.AppendLine("ТАБЛИЦА СВЁРТКИ");
        const int rowLabelWidth = 12;
        const int columnWidth = 11;

        text.Append(new string(' ', rowLabelWidth));
        for (int step = 0; step < MergeSteps.Count; step++)
        {
            text.Append($"Шаг {step}".PadLeft(columnWidth));
        }

        text.AppendLine();
        text.AppendLine(new string('-', rowLabelWidth + MergeSteps.Count * columnWidth));

        int rowCount = MergeSteps[0].Count;
        for (int row = 0; row < rowCount; row++)
        {
            text.Append($"строка {row}".PadRight(rowLabelWidth));
            foreach (IReadOnlyList<decimal> column in MergeSteps)
            {
                text.Append(row < column.Count
                    ? FormatProbability(column[row]).PadLeft(columnWidth)
                    : new string(' ', columnWidth));
            }

            text.AppendLine();
        }

        text.AppendLine();
        text.AppendLine("ОБЪЕДИНЕНИЯ УЗЛОВ");
        foreach (HuffmanMerge merge in Merges)
        {
            text.AppendLine(
                $"Шаг {merge.Step}: {FormatNode(merge.First)} + " +
                $"{FormatNode(merge.Second)} = {FormatNode(merge.Result)}");
        }

        text.AppendLine();
        text.AppendLine("КОДОВАЯ ТАБЛИЦА");
        text.AppendLine("Символ   Вероятность   Код       Длина");
        foreach (SymbolProbability item in Symbols)
        {
            string code = Codes[item.Symbol];
            text.AppendLine(
                $"{item.Symbol,-8} {FormatProbability(item.Probability),11}   " +
                $"{code,-9} {code.Length}");
        }

        text.AppendLine();
        text.AppendLine("КОДОВОЕ ДЕРЕВО");
        text.AppendLine(
            "Больший узел -> 0, меньший узел -> 1; " +
            "при равенстве сохраняется порядок таблицы");
        AppendTree(text, Root);

        text.AppendLine();
        text.AppendLine("СРЕДНЯЯ ДЛИНА КОДА");
        text.AppendLine($"L = {AverageCodeLength:F3} бит/символ");

        return text.ToString().TrimEnd();
    }

    // Текстовое кодовое дерево
    private static void AppendTree(StringBuilder text, HuffmanNode root)
    {
        text.AppendLine($"Корень: узел p = {FormatProbability(root.Probability)}");
        AppendTreeNode(text, root.Left!, string.Empty, false, "0", "0");
        AppendTreeNode(text, root.Right!, string.Empty, true, "1", "1");
    }

    // Узел текстового кодового дерева
    private static void AppendTreeNode(
        StringBuilder text,
        HuffmanNode node,
        string indent,
        bool isLast,
        string edge,
        string code)
    {
        string branch = isLast ? "└─" : "├─";
        string description = node.IsLeaf
            ? $"{node.Symbol}: p = {FormatProbability(node.Probability)}, код = {code}"
            : $"узел p = {FormatProbability(node.Probability)}";

        text.AppendLine($"{indent}{branch} {edge} -> {description}");

        if (node.IsLeaf)
        {
            return;
        }

        string childIndent = indent + (isLast ? "   " : "│  ");
        AppendTreeNode(text, node.Left!, childIndent, false, "0", code + "0");
        AppendTreeNode(text, node.Right!, childIndent, true, "1", code + "1");
    }

    // Форматирование вероятности
    private static string FormatProbability(decimal value) =>
        value.ToString("F3", CultureInfo.CurrentCulture);

    // Форматирование узла дерева
    private static string FormatNode(HuffmanNode node) =>
        $"{node.Label} ({FormatProbability(node.Probability)})";
}
