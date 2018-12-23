using Oracle.DataAccess.Client;

namespace TFlexOmpFix
{
    public class UserRepository
    {
        public UserRepository()
        {
        }

        public decimal GetUser(string username)
        {
            OracleCommand cmd = new OracleCommand();
            cmd.CommandText = "select code from omp_adm.user_list where fullname = :fullname";
            cmd.Connection = Connection.GetInstance();
            cmd.Parameters.Add("fullname", username);

            OracleDataReader rdr = cmd.ExecuteReader();
            if (!rdr.Read()) throw new OmpUserNotFoundException(username);

            return rdr.GetDecimal(0);
        }
    }
}