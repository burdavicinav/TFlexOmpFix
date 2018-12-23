using System;

namespace TFlexOmpFix
{
    public class OmpUserNotFoundException : Exception
    {
        public string FullName { get; set; }

        public OmpUserNotFoundException() : base()
        {
        }

        public OmpUserNotFoundException(string fullname) : this()
        {
            FullName = fullname;
        }

        public override string Message
        {
            get
            {
                return string.Format("Пользователь {0} не найден в КИС \"Омега\"", FullName);
            }
        }
    }
}