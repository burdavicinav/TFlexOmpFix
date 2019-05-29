using Oracle.DataAccess.Client;

namespace TFlexOmpFix.Procedure
{
    public static class CreateSpecDraw
    {
        private static OracleCommand command;

        public static void Exec(
            string sign,
            string name,
            decimal owner,
            decimal state,
            decimal user,
            ref decimal code)
        {
            if (command == null)
            {
                OracleParameter p_sign = new OracleParameter("p_sign", OracleDbType.Varchar2);
                OracleParameter p_name = new OracleParameter("p_name", OracleDbType.Varchar2);
                OracleParameter p_owner = new OracleParameter("p_owner", OracleDbType.Decimal);
                OracleParameter p_state = new OracleParameter("p_state", OracleDbType.Decimal);
                OracleParameter p_user = new OracleParameter("p_user", OracleDbType.Decimal);
                OracleParameter p_code = new OracleParameter(
                    "p_code",
                    OracleDbType.Decimal
                    );
                p_code.Direction = System.Data.ParameterDirection.Output;

                command = new OracleCommand();
                command.Connection = Connection.GetInstance();
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.CommandText = "omp_adm.pkg_sepo_tflex_synch_omp.create_spec_draw";

                command.Parameters.AddRange(
                    new OracleParameter[]
                    {
                            p_sign,
                            p_name,
                            p_owner,
                            p_state,
                            p_user,
                            p_code
                    });
            }

            OracleParameterCollection pars = command.Parameters;

            pars["p_sign"].Value = sign;
            pars["p_name"].Value = name;
            pars["p_owner"].Value = owner;
            pars["p_state"].Value = state;
            pars["p_user"].Value = user;

            command.ExecuteNonQuery();

            decimal.TryParse(pars["p_code"].Value.ToString(), out code);
        }
    }
}