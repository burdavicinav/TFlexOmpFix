using Oracle.DataAccess.Client;

namespace TFlexOmpFix.Procedure
{
    public static class AddElement
    {
        private static OracleCommand command;

        private static void Init()
        {
            OracleParameter p_spc = new OracleParameter("p_spc", OracleDbType.Decimal);
            OracleParameter p_elem = new OracleParameter("p_elem", OracleDbType.Decimal);
            OracleParameter p_type = new OracleParameter("p_type", OracleDbType.Decimal);
            OracleParameter p_section = new OracleParameter("p_section", OracleDbType.Decimal);
            OracleParameter p_cnt = new OracleParameter("p_cnt", OracleDbType.Decimal);
            OracleParameter p_position = new OracleParameter("p_position", OracleDbType.Varchar2);
            OracleParameter p_user = new OracleParameter("p_user", OracleDbType.Decimal);

            command = new OracleCommand
            {
                CommandType = System.Data.CommandType.StoredProcedure,
                CommandText = "omp_adm.pkg_sepo_tflex_synch_omp.add_element"
            };

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

        public static void Exec(
            decimal spc,
            decimal elem,
            decimal type,
            decimal section,
            decimal cnt,
            string position,
            decimal user)
        {
            if (command == null)
            {
                Init();
            }

            var pars = command.Parameters;

            pars["p_spc"].Value = spc;
            pars["p_elem"].Value = elem;
            pars["p_type"].Value = type;
            pars["p_section"].Value = section;
            pars["p_cnt"].Value = cnt;
            pars["p_position"].Value = position;
            pars["p_user"].Value = user;

            command.Connection = Connection.GetInstance();
            command.ExecuteNonQuery();
        }
    }
}