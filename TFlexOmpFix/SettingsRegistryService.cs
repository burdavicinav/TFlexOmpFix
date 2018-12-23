using Microsoft.Win32;
using System.Text;

namespace TFlexOmpFix
{
    /// <summary>
    /// Класс настроек
    /// </summary>
    public class SettingsRegistryService : ISettings
    {
        private ICryptoAlg iCryptoAlg;

        private RegistryKey GetSubKey(RegistryKey key, string subKey)
        {
            RegistryKey sKey = key.OpenSubKey(subKey, true);
            if (sKey == null)
            {
                sKey = key.CreateSubKey(subKey);
            }

            return sKey;
        }

        public SettingsRegistryService(ICryptoAlg alg)
        {
            iCryptoAlg = alg;
        }

        public SettingsRegistryService() : this(new SettingCryptoAlg1())
        {
        }

        /// <summary>
        /// Чтение настроек из реестра
        /// </summary>
        public Settings Read()
        {
            Settings settings = new Settings();

            // Раздел реестра HKEY_CURRENT USER
            RegistryKey currentUserKey = Registry.CurrentUser;

            // Software
            RegistryKey softwareKey = GetSubKey(currentUserKey, "Software");

            // SEPO TFlexToOmp
            RegistryKey tFlexToOmpKey = GetSubKey(softwareKey, "SEPO TFlexToOmp");

            // Connection
            RegistryKey connectionKey = GetSubKey(tFlexToOmpKey, "Connection");

            // ФИО
            object username = connectionKey.GetValue("Username");
            if (username != null) settings.UserName = username.ToString();

            // SID
            object sid = connectionKey.GetValue("SID");
            if (sid != null) settings.SID = sid.ToString();

            // User
            object login = connectionKey.GetValue("Login");
            if (login != null) settings.Login = login.ToString();

            // Password
            object password = connectionKey.GetValue("Passw");
            if (password != null)
            {
                string passw = Encoding.Default.GetString(password as byte[]);
                settings.Passw = iCryptoAlg.Decoding(passw);
            }

            return settings;
        }

        /// <summary>
        /// Запись настроек в реестр
        /// </summary>
        public void Write(Settings settings)
        {
            // Раздел реестра HKEY_CURRENT USER
            RegistryKey currentUserKey = Registry.CurrentUser;

            // Software
            RegistryKey softwareKey = GetSubKey(currentUserKey, "Software");

            // SEPO TFlexToOmp
            RegistryKey tFlexToOmpKey = GetSubKey(softwareKey, "SEPO TFlexToOmp");

            // Connection
            RegistryKey connectionKey = GetSubKey(tFlexToOmpKey, "Connection");

            // ФИО
            connectionKey.SetValue("Username", settings.UserName);

            // SID
            connectionKey.SetValue("SID", settings.SID);

            // User
            connectionKey.SetValue("Login", settings.Login);

            // Password
            if (settings.Passw != null)
            {
                string passw = iCryptoAlg.Encoding(settings.Passw);
                connectionKey.SetValue("Passw", Encoding.Default.GetBytes(passw));
            }
        }
    }
}