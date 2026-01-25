
using Microsoft.Extensions.Logging;

namespace PokemonStorage.SaveContent;

class Generation3Section
{
    public byte[] Data { get; set; }
    public ushort SectionId { get; set; }
    public ushort Checksum { get; set; }
    public uint Signature { get; set; }
    public uint SaveIndex { get; set; }

    public Generation3Section(byte[] content)
    {
        Data = Utility.GetBytes(content, 0, 3968);
        SectionId = Utility.GetUnsignedNumber<ushort>(content, 0xFF4, 2);
        Checksum = Utility.GetUnsignedNumber<ushort>(content, 0x0FF6, 2);
        Signature = Utility.GetUnsignedNumber<uint>(content, 0xFF8, 4);
        SaveIndex = Utility.GetUnsignedNumber<uint>(content, 0x0FFC, 4);
        
        Checksum checksumEvaluation = new()
        {
            Real = Checksum,
            Calculated = GetCalculatedChecksum()
        };

        Program.Logger.LogInformation($"1-Real:{Convert.ToString(checksumEvaluation.Real, 2)}");
        Program.Logger.LogInformation($"1-Calc:{Convert.ToString(checksumEvaluation.Calculated, 2)}");
    }

    public byte[] GetBytes()
    {
        byte[] bytes = new byte[3980];
        Array.Fill<byte>(bytes, 0x00);

        Buffer.BlockCopy(Data, 0, bytes, 0, 3968);
        byte[] SectionIdData = [.. BitConverter.GetBytes(SectionId)];
        Buffer.BlockCopy(SectionIdData, 0, bytes, 0x0FF4, 2);

        byte[] checksumData = [.. BitConverter.GetBytes(GetCalculatedChecksum())];
        Buffer.BlockCopy(checksumData, 0, bytes, 0x0FF4, 2);

        byte[] signatureData = [.. BitConverter.GetBytes(Signature)];
        Buffer.BlockCopy(signatureData, 0, bytes, 0x0FF4, 4);

        byte[] saveIndexData = [.. BitConverter.GetBytes(SaveIndex)];
        Buffer.BlockCopy(saveIndexData, 0, bytes, 0x0FF4, 4);

        return bytes;
    }

    public ushort GetCalculatedChecksum()
    {
        uint calculated = 0;
        for (int i = 0; i < 3968; i+=4)
        {
            uint part = Utility.GetUnsignedNumber<uint>(Data, i, 4);
            unchecked
            {
                calculated += part;
            }
        }
        return (ushort)((calculated >> 16) + (calculated & 0xFFFF));
    }
}