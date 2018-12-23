using Oracle.DataAccess.Client;
using System;

namespace TFlexOmpFix
{
    public class BORepository
    {
        public BORepository()
        {
        }

        public decimal GetState(decimal type, string state)
        {
            OracleCommand cmd = new OracleCommand();
            cmd.CommandText = "select name from omp_adm.businessobj_types where code = :code";
            cmd.Connection = Connection.GetInstance();

            cmd.Parameters.Add("code", type);

            string typeName = String.Empty;

            using (OracleDataReader rd = cmd.ExecuteReader())
            {
                rd.Read();
                typeName = rd.GetString(0);
            }

            cmd = new OracleCommand();
            cmd.CommandText =
                @"select code from omp_adm.businessobj_states
                    where name = :name and botype = :botype";

            cmd.Connection = Connection.GetInstance();
            cmd.Parameters.Add("name", state);
            cmd.Parameters.Add("botype", type);

            OracleDataReader rdr = cmd.ExecuteReader();
            if (!rdr.Read()) throw new StateNotFoundException(typeName, state);

            return rdr.GetDecimal(0);
        }

        public decimal GetType(string name)
        {
            OracleCommand cmd = new OracleCommand();
            cmd.CommandText = "select code from omp_adm.businessobj_types where name = :name";
            cmd.Connection = Connection.GetInstance();

            cmd.Parameters.Add("name", name);

            decimal typeCode = 0;

            using (OracleDataReader rd = cmd.ExecuteReader())
            {
                rd.Read();
                typeCode = rd.GetDecimal(0);
            }

            return typeCode;
        }
    }
}