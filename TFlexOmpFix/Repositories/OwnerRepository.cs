using Oracle.DataAccess.Client;
using TFlexOmpFix.Exceptions;

namespace TFlexOmpFix
{
    public class OwnerRepository
    {
        public OwnerRepository()
        {
        }

        public decimal GetOwner(string owner)
        {
            OracleCommand cmd = new OracleCommand();
            cmd.CommandText = "select owner from omp_adm.owner_name where name = :own";
            cmd.Connection = Connection.GetInstance();
            cmd.Parameters.Add("own", owner);

            OracleDataReader rdr = cmd.ExecuteReader();
            if (!rdr.Read()) throw new OwnerNotFoundException(owner);

            return rdr.GetDecimal(0);
        }

        public decimal GetOwnerByUser(decimal userId)
        {
            OracleCommand cmd = new OracleCommand();
            cmd.CommandText = "select owner from omp_adm.user_list where code = :userid";
            cmd.Connection = Connection.GetInstance();
            cmd.Parameters.Add("userid", userId);

            OracleDataReader rdr = cmd.ExecuteReader();
            if (!rdr.Read()) throw new UserOwnerNotFoundException();

            return rdr.GetDecimal(0);
        }
    }
}