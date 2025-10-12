using System.Text.RegularExpressions;
using TradingStrategyAPI.DataLoader.Models;

namespace TradingStrategyAPI.DataLoader.Services;

/// <summary>
/// Maps contract codes to base symbols and parses contract filenames.
/// </summary>
public partial class ContractMapper
{
    private static readonly Dictionary<string, string> ContractMappings = new()
    {
        { "ENQ", "NQ" },  // E-mini Nasdaq
        { "EES", "ES" },  // E-mini S&P 500
        { "EYM", "YM" },  // E-mini Dow
        { "MBT", "BTC" }, // Micro Bitcoin
        { "CL", "CL" }    // Crude Oil
    };

    /// <summary>
    /// Regex pattern for parsing contract filenames.
    /// Pattern: {CONTRACT}{EXPIRY} - {TIMEFRAME} - {SESSION}.csv
    /// Example: ENQZ25 - 1 min - RTH.csv
    /// </summary>
    [GeneratedRegex(@"^(E[A-Z]{2}|MBT|CL)([FGHJKMNQUVXZ]\d{2})\s*-\s*(.+?)\s*-\s*(RTH|ETH)\.csv$", RegexOptions.IgnoreCase)]
    private static partial Regex FilenameRegex();

    /// <summary>
    /// Gets the base symbol for a contract code.
    /// </summary>
    /// <param name="contractCode">Contract code (e.g., "ENQ", "EES")</param>
    /// <returns>Base symbol (e.g., "NQ", "ES")</returns>
    public string GetBaseSymbol(string contractCode)
    {
        if (ContractMappings.TryGetValue(contractCode.ToUpperInvariant(), out var symbol))
        {
            return symbol;
        }

        throw new ArgumentException($"Unknown contract code: {contractCode}. " +
            $"Supported codes: {string.Join(", ", ContractMappings.Keys)}");
    }

    /// <summary>
    /// Parses a contract filename and extracts contract information.
    /// </summary>
    /// <param name="filename">CSV filename (e.g., "ENQZ25 - 1 min - RTH.csv")</param>
    /// <returns>Parsed contract information</returns>
    public ContractInfo ParseContractFilename(string filename)
    {
        var match = FilenameRegex().Match(Path.GetFileName(filename));

        if (!match.Success)
        {
            throw new FormatException(
                $"Invalid filename format: {filename}\n\n" +
                $"Expected format: {{CONTRACT}}{{EXPIRY}} - {{TIMEFRAME}} - {{SESSION}}.csv\n" +
                $"Examples:\n" +
                $"  - ENQZ25 - 1 min - RTH.csv\n" +
                $"  - EESZ25 - 15 sec - ETH.csv\n" +
                $"  - CLZ25 - 1 min - RTH.csv\n\n" +
                $"Supported contract codes: {string.Join(", ", ContractMappings.Keys)}\n" +
                $"Supported expiry codes: F,G,H,J,K,M,N,Q,U,V,X,Z (followed by 2-digit year)\n" +
                $"Supported sessions: RTH (Regular Trading Hours), ETH (Extended Trading Hours)");
        }

        var contractPrefix = match.Groups[1].Value.ToUpperInvariant();
        var expiry = match.Groups[2].Value.ToUpperInvariant();
        var timeframe = match.Groups[3].Value;
        var session = match.Groups[4].Value.ToUpperInvariant();

        var contractCode = contractPrefix + expiry;
        var baseSymbol = GetBaseSymbol(contractPrefix);

        return new ContractInfo
        {
            ContractCode = contractCode,
            BaseSymbol = baseSymbol,
            ExpiryMonth = expiry,
            Timeframe = timeframe,
            Session = session,
            FilePath = filename
        };
    }

    /// <summary>
    /// Validates if a contract code prefix is supported.
    /// </summary>
    public bool IsValidContractCode(string contractCode)
    {
        return ContractMappings.ContainsKey(contractCode.ToUpperInvariant());
    }

    /// <summary>
    /// Gets all supported contract codes.
    /// </summary>
    public IEnumerable<string> GetSupportedCodes()
    {
        return ContractMappings.Keys;
    }
}
