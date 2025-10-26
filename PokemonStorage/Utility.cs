using System.Data.Common;

namespace PokemonStorage;

/// <summary>
/// A static container class that contains a variety of useful functions.
/// </summary>
public static class Utility
{
    #region Normalization

    /// <summary>
    /// Truncate the string if it is more than the specified length.
    /// </summary>
    /// <param name="input">String to truncate</param>
    /// <param name="max">Maximum allowable length</param>
    /// <returns>Truncated input string if it is longer than the max length, or the (trimmed) input string if its length is less than max</returns>
    public static string TruncateString(string input, int max)
    {
        if (string.IsNullOrWhiteSpace(input) || max <= 0)
        {
            return "";
        }

        return input.Trim().Length <= max ? input : input.Trim()[..max];
    }

    /// <summary>
    /// Capitialize the first letter of each word in the string.
    /// </summary>
    /// <param name="input">String to format</param>
    /// <returns>String With The First Letter Of Each Word Capitalized</returns>
    public static string ProperCapitalizeString(string input)
    {
        if (input is null)
        {
            return "";
        }

        string output = input.ToLower().Trim();
        return output.Length switch
        {
            0 => "",
            1 => output.ToUpper(),
            _ => string.Concat(output[0].ToString().ToUpper(), output[1..]),
        };
    }

    /// <summary>
    /// Converts smart quotes to regular quote characters.
    /// </summary>
    /// <param name="input">String with smart quotes</param>
    /// <returns>String with regular quotes</returns>
    public static string NormalizeQuotes(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "";
        }

        return input.Replace('\u2018', '\'').Replace('\u2019', '\'').Replace('\u201c', '\"').Replace('\u201d', '\"');
    }

    #endregion

    #region Bits and Bytes

    public static byte GetBit(byte data, byte index, bool isLittleEndian = false, bool isSigned = false)
    {
        return Convert.ToByte((data & (1 << (index - 1))) != 0);
    }

    public static int GetInt(byte[] data, int offset, int length, bool isLittleEndian=false, bool isSigned=false)
    {
        return BitConverter.ToInt32(data, offset) >> length;
    }
    
    #endregion
}