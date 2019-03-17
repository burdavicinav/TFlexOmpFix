using System;

namespace TFlexOmpFix
{
    public interface ILogging
    {
        void Write(TimeSpan span, string user, string doc, string section, string position,
            string sign, string name, decimal? qty, string doccode, string filePath, string log);

        void Close();
    }
}