using System.Collections.Generic;

namespace TFlexOmpFix
{
    public class SettingCryptoAlg1 : ICryptoAlg
    {
        private const byte control = 0x55;

        public string Encoding(string str)
        {
            List<byte> bytes = new List<byte>();
            byte stepByte = control;

            foreach (byte b in System.Text.Encoding.Default.GetBytes(str))
            {
                stepByte ^= b;
                bytes.Add(stepByte);
            }

            string passw = System.Text.Encoding.Default.GetString(bytes.ToArray());

            return passw;
        }

        public string Decoding(string str)
        {
            List<byte> passw_base = new List<byte>();

            byte[] bytes = System.Text.Encoding.Default.GetBytes(str);
            byte stepByte = control;

            foreach (var b in bytes)
            {
                stepByte ^= b;

                passw_base.Add(stepByte);
                stepByte = b;
            }

            return System.Text.Encoding.Default.GetString(passw_base.ToArray());
        }
    }
}