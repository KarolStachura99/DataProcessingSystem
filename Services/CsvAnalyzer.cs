using System.Globalization;

namespace DataProcessingSystem.Services;

public record CsvAnalysisResult(
    int RowCount,
    int TotalQuantity,
    decimal TotalRevenue,
    string TopProduct,
    Dictionary<string, decimal> RevenueByProduct);

public static class CsvAnalyzer
{
    public static CsvAnalysisResult Analyze(string csvContent)
    {
        var lines = csvContent.Split('\n',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (lines.Length < 2)
        {
            throw new FormatException("Plik musi zawierać nagłówek i co najmniej jeden wiersz danych.");
        }

        var revenueByProduct = new Dictionary<string, decimal>();
        var totalQuantity = 0;
        var totalRevenue = 0m;

        for (var i = 1; i < lines.Length; i++)
        {
            var columns = lines[i].Split(',');

            if (columns.Length != 3)
            {
                throw new FormatException($"Wiersz {i + 1}: oczekiwano 3 kolumn, znaleziono {columns.Length}.");
            }

            var product = columns[0].Trim();

            if (!int.TryParse(columns[1], out var quantity))
            {
                throw new FormatException($"Wiersz {i + 1}: niepoprawna ilość '{columns[1]}'.");
            }

            if (!decimal.TryParse(columns[2], NumberStyles.Number, CultureInfo.InvariantCulture, out var price))
            {
                throw new FormatException($"Wiersz {i + 1}: niepoprawna cena '{columns[2]}'.");
            }

            var revenue = quantity * price;
            totalQuantity += quantity;
            totalRevenue += revenue;
            revenueByProduct[product] = revenueByProduct.GetValueOrDefault(product) + revenue;
        }

        var topProduct = revenueByProduct.MaxBy(p => p.Value).Key;

        return new CsvAnalysisResult(lines.Length - 1, totalQuantity, totalRevenue, topProduct, revenueByProduct);
    }
}