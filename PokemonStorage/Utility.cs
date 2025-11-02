using System.Numerics;
using System.Text;

namespace PokemonStorage;

/// <summary>
/// A simple hextuple with naming for the 6 stat types
/// </summary>
public struct StatHextuple
{
    public ushort HP;
    public ushort Attack;
    public ushort Defense;
    public ushort SpecialAttack;
    public ushort SpecialDefense;
    public ushort Speed;

    public StatHextuple()
    {
        HP = Attack = Defense = SpecialAttack = SpecialDefense = Speed = 0;
    }

    public StatHextuple(ushort hp, ushort att, ushort def, ushort spa, ushort spd, ushort spe)
    {
        HP = hp;
        Attack = att;
        Defense = def;
        SpecialAttack = spa;
        SpecialDefense = spd;
        Speed = spe;
    }

    public StatHextuple(int hp, int att, int def, int spa, int spd, int spe)
    {
        HP = (ushort)hp;
        Attack = (ushort)att;
        Defense = (ushort)def;
        SpecialAttack = (ushort)spa;
        SpecialDefense = (ushort)spd;
        Speed = (ushort)spe;
    }

    public StatHextuple(double hp, double att, double def, double spa, double spd, double spe)
    {
        HP = (ushort)hp;
        Attack = (ushort)att;
        Defense = (ushort)def;
        SpecialAttack = (ushort)spa;
        SpecialDefense = (ushort)spd;
        Speed = (ushort)spe;
    }

        public StatHextuple(decimal hp, decimal att, decimal def, decimal spa, decimal spd, decimal spe)
    {
        HP = (ushort)hp;
        Attack = (ushort)att;
        Defense = (ushort)def;
        SpecialAttack = (ushort)spa;
        SpecialDefense = (ushort)spd;
        Speed = (ushort)spe;
    }
}

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

    public static byte GetBit(byte data, byte index, bool isBigEndian = false)
    {
        return Convert.ToByte((data & (1 << (index - 1))) != 0);
    }

    public static byte GetByte(byte[] data, int offset)
    {
        return data[offset];
    }

    /// <summary>
    /// Get an unsigned number from bytes in an array of bytes
    /// </summary>
    /// <typeparam name="T">Unsigned number datatype</typeparam>
    /// <param name="data">Array of bytes</param>
    /// <param name="offset">Start position</param>
    /// <param name="bytes">Number of bytes in value</param>
    /// <param name="isBigEndian">Should the bytes be read in big-endian</param>
    /// <returns>Unsigned number based on byte array</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static T GetUnsignedNumber<T>(byte[] data, int offset, int bytes, bool isBigEndian = false) where T : IUnsignedNumber<T>
    {
        if (data == null) return T.Zero;

        if (offset < 0 || bytes > 8 || offset + bytes > data.Length)
            throw new ArgumentOutOfRangeException("Invalid offset or number of bytes.", $"data[{data.Length}]@{offset}:{bytes}");

        byte[] resultBytes = new byte[8];
        Array.Fill<byte>(resultBytes, 0);
        Array.Copy(data, offset, resultBytes, 8 - bytes, bytes);

        ulong resultNumber =
            (BitConverter.IsLittleEndian == isBigEndian)
            ? BitConverter.ToUInt64([.. resultBytes.Reverse()])
            : BitConverter.ToUInt64(resultBytes.Reverse().ToArray());

        return (T)Convert.ChangeType(resultNumber, typeof(T));
    }

    public static byte[] GetBytes(byte[] data, int offset, int length, bool isBigEndian = false)
    {
        if (data == null) return [];

        if (offset < 0 || length < 0 || offset + length > data.Length)
            throw new ArgumentOutOfRangeException(nameof(offset), "Invalid offset or length.");

        byte[] result = new byte[length];
        Array.Copy(data, offset, result, 0, length);

        if (isBigEndian)
            return [.. result.Reverse()];
        else
            return result;
    }

    public static string GetEncodedString(byte[] data, int version, string lang)
    {
        StringBuilder result = new StringBuilder();

        if (version is >= 1 and <= 7)
        {
            List<byte> nullTerminators;
            switch (version)
            {
                case 1:
                case 2:
                case 3:
                case 4:
                    nullTerminators = [0x00, 0x50];
                    break;
                default:
                    nullTerminators = [0xFF, 0xFE];
                    break;
            }
            ;

            foreach (var b in data)
            {
                string c = GetEncodedCharacter(b, version);
                if (!nullTerminators.Contains(b))
                    result.Append(c);
                else
                    break;
            }
        }
        else if (version is >= 8 and <= 10)
        {
            for (int i = 0; i < data.Length; i++)
            {
                if (i % 2 == 0)
                {
                    ushort charBytes = GetUnsignedNumber<ushort>(data, i, 2);
                    string ch = GetEncodedCharacter(charBytes, version);
                    if (charBytes != 0xFFFF)
                        result.Append(ch);
                    else
                        break;
                }
            }
        }

        return result.ToString();
    }

    public static string GetEncodedCharacter(ushort character, int generation)
    {
        switch (generation)
        {
            case 1:
            case 2:
                return CharacterEncoding.EN_GEN1.GetValueOrDefault(character, "");
            case 3:
            case 4:
                return CharacterEncoding.EN_GEN2.GetValueOrDefault(character, "");
            case 5:
            case 6:
            case 7:
                return CharacterEncoding.WEST_GEN3.GetValueOrDefault(character, "");
            case 8:
            case 9:
            case 10:
                return CharacterEncoding.WEST_GEN4.GetValueOrDefault(character, "");
            default:
                return "?";
        }
    }

    #endregion
}