using Oracle.DataAccess.Client;

namespace TFlexOmpFix
{
    public class Connection
    {
        private static OracleConnection connection;

        private static OracleTransaction transaction;

        private Connection()
        {
        }

        public static void SetInstance(Settings settings)
        {
            if (!settings.IsCorrect) throw new SettingsNotValidException();

            // Соединение с Oracle
            OracleConnectionStringBuilder sbr = new OracleConnectionStringBuilder();
            sbr.DataSource = settings.SID;
            sbr.UserID = settings.Login;
            sbr.Password = settings.Passw;

            connection = new OracleConnection(sbr.ToString());
            connection.Open();
        }

        public static OracleConnection GetInstance()
        {
            return connection;
        }

        public static void OpenTransaction()
        {
            if (connection != null) transaction = connection.BeginTransaction();
        }

        public static void Commit()
        {
            if (transaction != null) transaction.Commit();
        }

        public static void Rollback()
        {
            if (transaction != null) transaction.Rollback();
        }

        public static void Close()
        {
            if (connection != null) connection.Close();
        }
    }
}