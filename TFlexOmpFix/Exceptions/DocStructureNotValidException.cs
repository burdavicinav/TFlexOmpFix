using System;

namespace TFlexOmpFix.Exceptions
{
    public class DocStructureNotValidException : Exception
    {
        public override string Message
        {
            get
            {
                return "Структура изделия некорректна для выполнения операции!";
            }
        }
    }
}