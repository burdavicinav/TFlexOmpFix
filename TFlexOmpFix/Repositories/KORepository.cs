using Oracle.DataAccess.Client;

namespace TFlexOmpFix
{
    public class KORepository
    {
        public decimal GetType(decimal botype)
        {
            OracleCommand cmd = new OracleCommand();
            cmd.CommandText = "select kotype from omp_adm.kotype_to_botype where botype = :botype";
            cmd.Connection = Connection.GetInstance();

            cmd.Parameters.Add("botype", botype);

            decimal typeCode = 0;

            using (OracleDataReader rd = cmd.ExecuteReader())
            {
                rd.Read();
                typeCode = rd.GetDecimal(0);
                typeCode = rd.GetDecimal(0);
            }

            return typeCode;
        }
    }
}