namespace TFlexOmpFix
{
    public class Settings
    {
        public string UserName { get; set; }

        public string SID { get; set; }

        public string Login { get; set; }

        public string Passw { get; set; }

        public string Owner { get; set; }

        public string State { get; set; }

        public bool UpdateRevision { get; set; }

        /// <summary>
        /// Проверка на корректность занесения настроек
        /// </summary>
        /// <returns>Возвращает валидность настроек</returns>
        public bool IsCorrect
        {
            get
            {
                return (UserName != null && SID != null && Login != null && Passw != null);
            }
        }
    }
}