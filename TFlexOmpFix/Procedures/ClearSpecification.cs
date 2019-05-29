using Oracle.DataAccess.Client;

namespace TFlexOmpFix.Procedure
{
    public static class ClearSpecification
    {
        private static OracleCommand command;

        public static void Exec(decimal spc)
        {
            if (command == null)
            {
                OracleParameter p_spc = new OracleParameter("p_spc", OracleDbType.Decimal);

                command = new OracleCommand();
                command.Connection = Connection.GetInstance();
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.CommandText = "omp_adm.pkg_sepo_tflex_synch_omp.clear_specification";

                command.Parameters.AddRange(
                    new OracleParameter[]
                    {
                    p_spc
                    });
            }

            var pars = command.Parameters;

            pars["p_spc"].Value = spc;
            command.ExecuteNonQuery();
        }
    }
}