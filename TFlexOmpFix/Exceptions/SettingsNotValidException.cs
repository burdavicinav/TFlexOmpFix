using System;

namespace TFlexOmpFix
{
    public class SettingsNotValidException : Exception
    {
        public SettingsNotValidException() : base()
        {
        }

        public override string Message
        {
            get
            {
                return "Проверьте меню настроек - не все поля заполнены!";
            }
        }
    }
}