using System.IO;
using System.Text;
using TFlexOmpFix;

namespace TFAGenerator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            FileStream tfa = File.Create("TFlexOmpFix.tfa");

            string guid = "[" + Lib.Guid.ToString().ToUpper() + "]";
            byte[] g_bts = Encoding.Default.GetBytes(guid);

            byte[] nl_bts = { 0x0d, 0x0a };

            string dll = "dll=TFlexOmpFix.dll";
            byte[] dll_bts = Encoding.Default.GetBytes(dll);

            string name = "name=" + Lib.Name;
            byte[] name_bts = Encoding.Default.GetBytes(name);

            string start = "autostart=1";
            byte[] start_bts = Encoding.Default.GetBytes(start);

            string mgd = "managed=1";
            byte[] mgd_bts = Encoding.Default.GetBytes(mgd);

            tfa.Write(g_bts, 0, g_bts.Length);
            tfa.Write(nl_bts, 0, nl_bts.Length);
            tfa.Write(dll_bts, 0, dll_bts.Length);
            tfa.Write(nl_bts, 0, nl_bts.Length);
            tfa.Write(name_bts, 0, name_bts.Length);
            tfa.Write(nl_bts, 0, nl_bts.Length);
            tfa.Write(start_bts, 0, start_bts.Length);
            tfa.Write(nl_bts, 0, nl_bts.Length);
            tfa.Write(mgd_bts, 0, mgd_bts.Length);
            tfa.Write(nl_bts, 0, nl_bts.Length);

            tfa.Close();
        }
    }
}