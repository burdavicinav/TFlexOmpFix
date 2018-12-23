using System.IO;

namespace TFlexOmpFix
{
    public class Log : ILogging
    {
        private StreamWriter log;

        public Log(string path)
        {
            log = new StreamWriter(path);
        }

        public void Write(string str)
        {
            log.WriteLine(str);
        }

        public void Close()
        {
            log.Close();
        }
    }
}