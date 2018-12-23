using System;

namespace TFlexOmpFix.Exceptions
{
    public class DocStructurePerentPropsException : Exception
    {
        public override string Message
        {
            get
            {
                return "Некорректные свойства спецификации!";
            }
        }
    }
}