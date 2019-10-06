using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TFlexOmpFix.Exceptions
{
    public class DocStructureNotExistsException : Exception
    {
        public string File { get; set; }

        public string Structure { get; set; }

        public DocStructureNotExistsException()
        {
        }

        public DocStructureNotExistsException(string file, string structure)
        {
            File = file;
            Structure = structure;
        }

        public override string Message
        {
            get
            {
                return String.Concat(
                    "Не найдена структура изделия \" ",
                    Structure,
                    "\" в файле ",
                    File);
            }
        }
    }
}