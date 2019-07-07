using Oracle.DataAccess.Client;

namespace TFlexOmpFix.Procedure
{
    public static class CreateFixture
    {
        private static OracleCommand command;

        public static void Exec(
            string sign,
            string name,
            decimal owner,
            decimal state,
            decimal user,
            decimal fixtype,
            ref decimal code)
        {
            if (command == null)
            {
                OracleParameter p_sign = new OracleParameter("p_sign", OracleDbType.Varchar2);
                OracleParameter p_name = new OracleParameter("p_name", OracleDbType.Varchar2);
                OracleParameter p_owner = new OracleParameter("p_owner", OracleDbType.Decimal);
                OracleParameter p_state = new OracleParameter("p_state", OracleDbType.Decimal);
                OracleParameter p_user = new OracleParameter("p_user", OracleDbType.Decimal);
                OracleParameter p_fixtype = new OracleParameter("p_fixtype", OracleDbType.Decimal);
                OracleParameter p_code = new OracleParameter(
                    "p_code",
                    OracleDbType.Decimal
                    );
                p_code.Direction = System.Data.ParameterDirection.Output;

                command = new OracleCommand();
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.CommandText = "omp_adm.pkg_sepo_tflex_synch_omp.create_fixture";

                command.Parameters.AddRange(
                    new OracleParameter[]
                    {
                            p_sign,
                            p_name,
                            p_owner,
                            p_state,
                            p_user,
                            p_fixtype,
                            p_code
                    });
            }

            OracleParameterCollection pars = command.Parameters;

            pars["p_sign"].Value = sign;
            pars["p_name"].Value = name;
            pars["p_owner"].Value = owner;
            pars["p_state"].Value = state;
            pars["p_user"].Value = user;
            pars["p_fixtype"].Value = fixtype;

            command.Connection = Connection.GetInstance();
            command.ExecuteNonQuery();

            decimal.TryParse(pars["p_code"].Value.ToString(), out code);
        }
    }
}