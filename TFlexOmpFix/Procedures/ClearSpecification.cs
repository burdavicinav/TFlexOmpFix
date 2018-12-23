using Oracle.DataAccess.Client;

namespace TFlexOmpFix.Procedure
{
    public class ClearSpecification : OracleProcedure
    {
        public ClearSpecification()
        {
            Scheme = "omp_adm";
            Package = "pkg_sepo_tflex_synch_omp";
            Name = "clear_specification";

            OracleParameter p_spc = new OracleParameter("p_spc", OracleDbType.Decimal);

            command = new OracleCommand();
            command.Connection = Connection.GetInstance();
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.CommandText = FullName;

            command.Parameters.AddRange(
                new OracleParameter[]
                {
                    p_spc
                });
        }

        public void Exec(
            decimal spc)
        {
            OracleParameterCollection pars = command.Parameters;

            pars["p_spc"].Value = spc;
            command.ExecuteNonQuery();
        }
    }
}