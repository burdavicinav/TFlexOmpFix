using System;

namespace TFlexOmpFix.Exceptions
{
    public class DocStructureDocTypeNotValidException : Exception
    {
        public string File { get; set; }

        public DocStructureDocTypeNotValidException()
        {
        }

        public DocStructureDocTypeNotValidException(string file) : this()
        {
            File = file;
        }

        public override string Message
        {
            get
            {
                return
                    String.Concat(
                        "У родительского элемента некорректно указан тип документа! ",
                        File);
            }
        }
    }
}