using Oracle.DataAccess.Client;

namespace TFlexOmpFix.Procedure
{
    internal class CreateDocument : OracleProcedure
    {
        public CreateDocument()
        {
            Scheme = "omp_adm";
            Package = "pkg_sepo_tflex_synch_omp";
            Name = "create_document";

            OracleParameter p_type = new OracleParameter("p_doctype", OracleDbType.Varchar2);
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
            command.CommandText = FullName;

            command.Parameters.AddRange(
                new OracleParameter[]
                {
                            p_type,
                            p_sign,
                            p_name,
                            p_owner,
                            p_state,
                            p_user,
                            p_code
                });
        }

        public void Exec(
            decimal doctype,
            string sign,
            string name,
            decimal owner,
            decimal state,
            decimal user,
            ref decimal code)
        {
            OracleParameterCollection pars = command.Parameters;

            pars["p_doctype"].Value = doctype;
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