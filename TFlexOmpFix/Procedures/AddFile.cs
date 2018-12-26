using Oracle.DataAccess.Client;
using System.IO;
using System.Security.Cryptography;

namespace TFlexOmpFix.Procedure
{
    public class AddFile : OracleProcedure
    {
        public AddFile()
        {
            Scheme = "omp_adm";
            Package = "pkg_sepo_tflex_synch_omp";
            Name = "add_file";

            OracleParameter p_code = new OracleParameter("p_code", OracleDbType.Decimal);
            OracleParameter p_fname = new OracleParameter("p_fname", OracleDbType.Varchar2);
            OracleParameter p_fhash = new OracleParameter("p_fhash", OracleDbType.Varchar2);
            OracleParameter p_file = new OracleParameter("p_file", OracleDbType.Blob);
            OracleParameter p_user = new OracleParameter("p_user", OracleDbType.Decimal);
            OracleParameter p_groupcode = new OracleParameter("p_groupcode", OracleDbType.Decimal);

            command = new OracleCommand();
            command.Connection = Connection.GetInstance();
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.CommandText = FullName;

            command.Parameters.AddRange(
                new OracleParameter[]
                {
                            p_code,
                            p_fname,
                            p_fhash,
                            p_file,
                            p_user,
                            p_groupcode
                });
        }

        public void Exec(
            decimal code,
            string path,
            decimal user,
            decimal? groupcode)
        {
            System.Windows.Forms.MessageBox.Show(path);

            OracleParameterCollection pars = command.Parameters;

            FileInfo file = new FileInfo(path);
            FileStream stream = file.OpenRead();

            if (stream != null)
            {
                // файл
                byte[] bts = new byte[stream.Length];
                stream.Read(bts, 0, (int)stream.Length);

                // хеш
                SHA1 sha = new SHA1CryptoServiceProvider();
                byte[] hash = sha.ComputeHash(bts);

                pars["p_code"].Value = code;
                pars["p_fname"].Value = file.Name;
                pars["p_fhash"].Value = (System.BitConverter.ToString(hash)).Replace("-", "").ToLower();
                pars["p_file"].Value = bts;
                pars["p_user"].Value = user;
                pars["p_groupcode"].Value = groupcode;

                command.ExecuteNonQuery();

                stream.Close();
            }
        }
    }
}