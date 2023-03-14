using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SayDos
{
    public enum Prefix
    {
        None,
        X,
        Dollar
    }
    public static class MyMath
    {
        private const char
            BYTE1 = '{',
            BYTE2 = '}';

        private const string pre_0X = "0x", pre_dollar = "$";

        public static string DecToHex(int i, Prefix prefix = Prefix.None, int length = 2)
        {
            string pre = "";
            if (prefix == Prefix.X)
            {
                pre = pre_0X;
            }
            else if (prefix == Prefix.Dollar)
            {
                pre = pre_dollar;
            }
            return pre + i.ToString("X" + length.ToString());
        }

        public static string DecToHex(long i, Prefix prefix = Prefix.None, int length = 2)
        {
            string pre = "";
            if (prefix == Prefix.X)
            {
                pre = pre_0X;
            }
            else if (prefix == Prefix.Dollar)
            {
                pre = pre_dollar;
            }
            return pre + i.ToString("X" + length.ToString());
        }

        public static string DecToHex(ulong i, Prefix prefix = Prefix.None, int length = 2) => DecToHex((int)i, prefix, length);

        public static string DecToHex(byte[] l, Prefix prefix = Prefix.None)
        {
            string s = "";

            if (prefix == Prefix.X)
            {
                s = pre_0X;
            }
            else if (prefix == Prefix.Dollar)
            {
                s = pre_dollar;
            }

            foreach (byte b in l)
            {
                s += b.ToString("X2");
            }

            return s;
        }

        public static string DecToHex(sbyte i, Prefix prefix = Prefix.None, int length = 2)
        {
            return DecToHex(i, prefix, length);
        }

        /*
         * You may get a error saying:
         * Additional non-parsable characters are at the end of the string.
         * 
         * Make sure not to copy and paste from the calculator.
         */
        public static int HexToDec(string s)
        {
            string hexToParse = RemovePrefix(s);
            return Convert.ToInt32(hexToParse, 16);
        }

        public static ulong HexToUInt64(string s)
        {
            string hexToParse = RemovePrefix(s);
            return Convert.ToUInt64(hexToParse, 16);
        }

        public static List<byte> HexToByteList(string s)
        {
            List<byte> b = new List<byte>();
            for (int i = 0; i < s.Length; i += 2)
            {
                b.Add((byte)HexToDec(s.Substring(i, 2)));
            }
            return b;
        }

        public static byte[] HexToBytes(string s)
        {
            byte[] b = new byte[s.Length / 2];
            for (int i = 0; i < s.Length; i += 2)
            {
                b[i / 2] = Convert.ToByte(s.Substring(i, 2), 16);
            }
            return b;
        }

        //Stack overflow: Check a string to see if all characters are hexidemical values
        public static bool IsHex(string text)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(text, @"(0x|$)[0-9a-fA-F]+");
        }

        public static string RemovePrefix(string text)
        {
            return text.Replace(pre_0X, "").Replace(pre_dollar, "");
        }

        public static int HasPrefix(string text)
        {
            int val = 0;
            if (text.Contains(pre_0X))
            {
                val = pre_0X.Length;
            }
            else if (text.Contains(pre_dollar))
            {
                val = pre_dollar.Length;
            }
            return val;
        }

        public static int GetValueOfBytes(params byte[] bb)
        {
            if (bb.Length != 1)
            {
                return BitConverter.ToInt16(bb, 0);
            }
            else
            {
                return bb[0];
            }
        }

        public static int GetValueOfBytes(params int[] bb)
        {
            byte[] b = new byte[bb.Length];
            int i = 0;
            foreach (int by in bb)
            {
                if (by < byte.MaxValue)
                {
                    b[i++] = (byte)by;
                }
                else
                {
                    throw new Exception("Value over " + DecToHex(byte.MaxValue, Prefix.X));
                }
            }
            return BitConverter.ToInt32(b, 0);
        }

        public static string FormatByteInBrackets(byte b) => BYTE1 + MyMath.DecToHex(b) + BYTE2;
        public static string FormatByteInBrackets(byte[] b) => BYTE1 + MyMath.DecToHex(b) + BYTE2;

        public static string FormatBytesInBrackets(byte[] b)
        {
            string s = "";

            foreach (byte ByteEntry in b)
            {
                s += BYTE1 + MyMath.DecToHex(ByteEntry) + BYTE2;
            }

            return s;
        }
    }
}
