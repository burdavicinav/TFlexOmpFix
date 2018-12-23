using Oracle.DataAccess.Client;

namespace TFlexOmpFix.Procedure
{
    public class AddElement : OracleProcedure
    {
        public AddElement()
        {
            Scheme = "omp_adm";
            Package = "pkg_sepo_tflex_synch_omp";
            Name = "add_element";

            OracleParameter p_spc = new OracleParameter("p_spc", OracleDbType.Decimal);
            OracleParameter p_elem = new OracleParameter("p_elem", OracleDbType.Decimal);
            OracleParameter p_type = new OracleParameter("p_type", OracleDbType.Decimal);
            OracleParameter p_section = new OracleParameter("p_section", OracleDbType.Decimal);
            OracleParameter p_cnt = new OracleParameter("p_cnt", OracleDbType.Decimal);
            OracleParameter p_position = new OracleParameter("p_position", OracleDbType.Varchar2);
            OracleParameter p_user = new OracleParameter("p_user", OracleDbType.Decimal);

            command = new OracleCommand();
            command.Connection = Connection.GetInstance();
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.CommandText = FullName;

            command.Parameters.AddRange(
                new OracleParameter[]
                {
                            p_spc,
                            p_elem,
                            p_type,
                            p_section,
                            p_cnt,
                            p_position,
                            p_user
                });
        }

        public void Exec(
            decimal spc,
            decimal elem,
            decimal type,
            decimal section,
            decimal cnt,
            string position,
            decimal user)
        {
            OracleParameterCollection pars = command.Parameters;

            pars["p_spc"].Value = spc;
            pars["p_elem"].Value = elem;
            pars["p_type"].Value = type;
            pars["p_section"].Value = section;
            pars["p_cnt"].Value = cnt;
            pars["p_position"].Value = position;
            pars["p_user"].Value = user;

            command.ExecuteNonQuery();
        }
    }
}