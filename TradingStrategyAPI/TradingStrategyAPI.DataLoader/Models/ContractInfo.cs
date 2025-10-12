namespace TradingStrategyAPI.DataLoader.Models;

/// <summary>
/// Information parsed from a contract CSV filename.
/// </summary>
public class ContractInfo
{
    /// <summary>
    /// Full contract code (e.g., "ENQZ25").
    /// </summary>
    public string ContractCode { get; set; } = string.Empty;

    /// <summary>
    /// Base symbol for the contract (e.g., "NQ").
    /// </summary>
    public string BaseSymbol { get; set; } = string.Empty;

    /// <summary>
    /// Expiry month code and year (e.g., "Z25" = December 2025).
    /// </summary>
    public string ExpiryMonth { get; set; } = string.Empty;

    /// <summary>
    /// Timeframe of the data (e.g., "1 min", "15 sec").
    /// </summary>
    public string Timeframe { get; set; } = string.Empty;

    /// <summary>
    /// Trading session (RTH = Regular Trading Hours, ETH = Extended Trading Hours).
    /// </summary>
    public string Session { get; set; } = string.Empty;

    /// <summary>
    /// Full path to the CSV file.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets a display-friendly description of this contract.
    /// </summary>
    public string GetDescription()
    {
        var expiryName = GetExpiryMonthName();
        return $"{BaseSymbol} {expiryName}, {Timeframe}, {Session}";
    }

    /// <summary>
    /// Converts expiry code to month name (e.g., "Z25" => "December 2025").
    /// </summary>
    private string GetExpiryMonthName()
    {
        if (string.IsNullOrEmpty(ExpiryMonth) || ExpiryMonth.Length < 2)
            return ExpiryMonth;

        var monthCode = ExpiryMonth[0];
        var year = "20" + ExpiryMonth[1..];

        var month = monthCode switch
        {
            'F' => "January",
            'G' => "February",
            'H' => "March",
            'J' => "April",
            'K' => "May",
            'M' => "June",
            'N' => "July",
            'Q' => "August",
            'U' => "September",
            'V' => "October",
            'X' => "November",
            'Z' => "December",
            _ => monthCode.ToString()
        };

        return $"{month} {year}";
    }
}
