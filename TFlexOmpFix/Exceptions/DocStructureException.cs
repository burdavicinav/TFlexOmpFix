using System;

namespace TFlexOmpFix.Exceptions
{
    public class DocStructureException : Exception
    {
        public string File { get; set; }

        public DocStructureException()
        {
        }

        public DocStructureException(string file)
        {
            File = file;
        }

        public override string Message
        {
            get
            {
                return String.Concat(
                    "Не найдена структура изделия, либо она не единственная! ",
                    File);
            }
        }
    }
}