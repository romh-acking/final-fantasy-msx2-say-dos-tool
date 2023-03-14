using Newtonsoft.Json;
using System;
using System.Linq;
using System.Text;

namespace SayDos
{
    public class SayDosRootDirectory
    {
        public const byte RD_SIZE = 0x10;
        public string FileName { get; set; }
        public string FileExt { get; set; }

        [JsonIgnore]
        public string File { get => FileName + FileExt; }

        [JsonConverter(typeof(HexStringToUshortJsonConverter))]
        public ushort StartSector { get; set; }
        [JsonConverter(typeof(HexStringToUshortJsonConverter))]
        public ushort EndSector { get; set; }
        [JsonConverter(typeof(HexStringToByteJsonConverter))]
        public byte SizeInSectors { get => (byte)(EndSector - StartSector + 1); }

        public bool IsEmpty { get; set; }
        [JsonConverter(typeof(HexStringToByteJsonConverter))]
        public byte ByteFill { get; set; }

        public SayDosRootDirectory(byte[] entryBytes)
        {
            var firstByte = entryBytes[0];
            IsEmpty = entryBytes.All(singleByte => singleByte == firstByte);

            if (IsEmpty)
            {
                ByteFill = firstByte;
                return;
            }

            FileName = Encoding.ASCII.GetString(entryBytes, 0x0, 0x8).Trim();
            FileExt = Encoding.ASCII.GetString(entryBytes, 0x8, 0x4).Trim();
            StartSector = BitConverter.ToUInt16(entryBytes, 0xC);
            EndSector = BitConverter.ToUInt16(entryBytes, 0xE);
        }

        public SayDosRootDirectory()
        {

        }

        public byte[] CreateRootDirectoryEntry()
        {
            if (IsEmpty)
            {
                return Enumerable.Repeat(ByteFill, RD_SIZE).ToArray();
            }

            return 
                Encoding.ASCII.GetBytes(FileName.PadRight(8)).Concat
                (Encoding.ASCII.GetBytes(FileExt.PadRight(3))).Concat
                (BitConverter.GetBytes(StartSector)).Concat
                (BitConverter.GetBytes(EndSector)).ToArray();
        }
    }

    public sealed class HexStringToUshortJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => typeof(ushort).Equals(objectType);
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => writer.WriteValue(MyMath.DecToHex((ushort)value, Prefix.X));
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) => (ushort)MyMath.HexToDec((string)reader.Value);
    }

    public sealed class HexStringToByteJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => typeof(byte).Equals(objectType);
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => writer.WriteValue(MyMath.DecToHex((byte)value, Prefix.X));
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) => (byte)MyMath.HexToDec((string)reader.Value);
    }
}