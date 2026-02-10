using System.Numerics;
using System.Text;

namespace Bluetide.Classes
{
    public class BaseEncoding
    {
        private const int LengthHeaderWidth = 6;

        public static string Encode(byte[] data)
        {
            byte[] tmp = new byte[data.Length + 1];
            Array.Copy(data, tmp, data.Length);

            BigInteger bi = new BigInteger(tmp);
            string number = bi.ToString();

            return data.Length.ToString().PadLeft(LengthHeaderWidth, '0') + number;
        }

        public static byte[] Decode(string s)
        {
            int length = int.Parse(s.Substring(0, LengthHeaderWidth));
            string numberPart = s.Substring(LengthHeaderWidth);

            BigInteger bi = BigInteger.Parse(numberPart);
            byte[] tmp = bi.ToByteArray();

            byte[] result = new byte[length];
            Array.Copy(tmp, result, Math.Min(length, tmp.Length));
            return result;
        }

        public static string EncodeText(string text)
            => Encode(Encoding.UTF8.GetBytes(text));

        public static string DecodeText(string s)
            => Encoding.UTF8.GetString(Decode(s));
    }
}
