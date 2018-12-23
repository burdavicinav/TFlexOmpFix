using System;

namespace TFlexOmpFix.Exceptions
{
    public class DocStructureParentSignNotValidException : Exception
    {
        public string File { get; set; }

        public DocStructureParentSignNotValidException()
        {
        }

        public DocStructureParentSignNotValidException(string file) : this()
        {
            File = file;
        }

        public override string Message
        {
            get
            {
                return
                    String.Concat(
                        "У родительского элемента не задано или некорректно обозначение! ",
                        File);
            }
        }
    }
}