using System;

namespace TFlexOmpFix.Exceptions
{
    public class UserOwnerNotFoundException : Exception
    {
        public UserOwnerNotFoundException() : base()
        {
        }

        public override string Message
        {
            get
            {
                return string.Format("У пользователя не указан владелец");
            }
        }
    }
}