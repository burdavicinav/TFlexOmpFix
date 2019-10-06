using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TFlexOmpFix.Exceptions
{
    public class DocStructureConfigurationsException : Exception
    {
        public string File { get; set; }

        public DocStructureConfigurationsException()
        {
        }

        public DocStructureConfigurationsException(string file)
        {
            File = file;
        }

        public override string Message
        {
            get
            {
                return String.Concat(
                    "У исполнения не задана структура изделия!",
                    File);
            }
        }
    }
}