using System.Text.RegularExpressions;

namespace TIAC4;

// Кодирование методом Хаффмана
public static class HuffmanCodec
{
    // Построение кода Хаффмана
    public static HuffmanResult Build(IReadOnlyList<SymbolProbability> symbols)
    {
        ArgumentNullException.ThrowIfNull(symbols);

        if (symbols.Count < 2)
        {
            throw new ArgumentException("Для построения кода нужны минимум два символа.");
        }

        if (symbols.Any(item => item.Probability <= 0))
        {
            throw new ArgumentException("Все вероятности должны быть больше нуля.");
        }

        if (symbols.Select(item => item.Symbol).Distinct(StringComparer.OrdinalIgnoreCase).Count()
            != symbols.Count)
        {
            throw new ArgumentException("Имена символов не должны повторяться.");
        }

        decimal probabilitySum = symbols.Sum(item => item.Probability);
        if (Math.Abs(probabilitySum - 1m) > 0.000001m)
        {
            throw new ArgumentException(
                $"Сумма вероятностей должна быть равна 1. Сейчас: {probabilitySum:F6}.");
        }

        int creationOrder = 0;
        var nodes = symbols
            .Select(item => new HuffmanNode(
                item.Probability,
                creationOrder++,
                item.Symbol,
                item.Symbol))
            .ToList();

        var merges = new List<HuffmanMerge>();
        var steps = new List<IReadOnlyList<decimal>>
        {
            CaptureDescendingProbabilities(nodes)
        };

        while (nodes.Count > 1)
        {
            SortForMerge(nodes);

            HuffmanNode first = nodes[0];
            HuffmanNode second = nodes[1];
            nodes.RemoveRange(0, 2);

            HuffmanNode left = first.Probability == second.Probability ? first : second;
            HuffmanNode right = ReferenceEquals(left, first) ? second : first;
            string label = ((char)('A' + merges.Count)).ToString();
            var merged = new HuffmanNode(
                first.Probability + second.Probability,
                creationOrder++,
                label,
                Left: left,
                Right: right);

            nodes.Add(merged);
            merges.Add(new HuffmanMerge(merges.Count + 1, first, second, merged));
            steps.Add(CaptureDescendingProbabilities(nodes));
        }

        HuffmanNode root = nodes[0];
        var codes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        BuildCodes(root, string.Empty, codes);

        double averageCodeLength = symbols.Sum(item =>
            (double)item.Probability * codes[item.Symbol].Length);

        return new HuffmanResult(
            symbols.ToArray(),
            steps,
            merges,
            codes,
            root,
            averageCodeLength);
    }

    // Кодирование последовательности символов
    public static string Encode(string message, HuffmanResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        string[] symbols = Regex.Split(message.Trim(), @"[\s,;]+")
            .Where(item => item.Length > 0)
            .Select(item => item.ToUpperInvariant())
            .ToArray();

        if (symbols.Length == 0)
        {
            throw new ArgumentException("Введите хотя бы один символ, например: S1 S2 S6.");
        }

        var encodedParts = new List<string>(symbols.Length);
        foreach (string symbol in symbols)
        {
            if (!result.Codes.TryGetValue(symbol, out string? code))
            {
                throw new ArgumentException(
                    $"Символ «{symbol}» отсутствует в кодовой таблице.");
            }

            encodedParts.Add(code);
        }

        return string.Concat(encodedParts);
    }

    // Декодирование битовой последовательности
    public static string Decode(string bits, HuffmanResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        string normalizedBits = new(bits.Where(character => !char.IsWhiteSpace(character)).ToArray());
        if (normalizedBits.Length == 0)
        {
            throw new ArgumentException("Введите битовую последовательность для декодирования.");
        }

        if (normalizedBits.Any(character => character is not ('0' or '1')))
        {
            throw new ArgumentException("Битовая последовательность может содержать только 0 и 1.");
        }

        var decodedSymbols = new List<string>();
        HuffmanNode current = result.Root;

        foreach (char bit in normalizedBits)
        {
            current = (bit == '0' ? current.Left : current.Right)
                ?? throw new ArgumentException("Последовательность не соответствует дереву Хаффмана.");

            if (current.IsLeaf)
            {
                decodedSymbols.Add(current.Symbol!);
                current = result.Root;
            }
        }

        if (!ReferenceEquals(current, result.Root))
        {
            throw new ArgumentException("Последовательность заканчивается внутри кодового слова.");
        }

        return string.Join(' ', decodedSymbols);
    }

    // Сортировка узлов перед свёрткой
    private static void SortForMerge(List<HuffmanNode> nodes)
    {
        nodes.Sort((first, second) =>
        {
            int byProbability = first.Probability.CompareTo(second.Probability);
            return byProbability != 0
                ? byProbability
                : first.CreationOrder.CompareTo(second.CreationOrder);
        });
    }

    // Снимок вероятностей текущего шага
    private static IReadOnlyList<decimal> CaptureDescendingProbabilities(
        IEnumerable<HuffmanNode> nodes) =>
        nodes
            .OrderByDescending(node => node.Probability)
            .ThenBy(node => node.CreationOrder)
            .Select(node => node.Probability)
            .ToArray();

    // Формирование кодовых слов
    private static void BuildCodes(
        HuffmanNode node,
        string prefix,
        IDictionary<string, string> codes)
    {
        if (node.IsLeaf)
        {
            codes[node.Symbol!] = prefix.Length > 0 ? prefix : "0";
            return;
        }

        BuildCodes(node.Left!, prefix + "0", codes);
        BuildCodes(node.Right!, prefix + "1", codes);
    }

}
