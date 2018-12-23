using System;

namespace TFlexOmpFix.Exceptions
{
    public class DocStructureParentException : Exception
    {
        public string File { get; set; }

        public DocStructureParentException()
        {
        }

        public DocStructureParentException(string file) : this()
        {
            File = file;
        }

        public override string Message
        {
            get
            {
                return
                    String.Concat(
                        "В структуре изделия не задан родительский элемент, либо он не единственный! ",
                        File);
            }
        }
    }
}