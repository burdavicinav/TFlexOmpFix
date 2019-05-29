using Oracle.DataAccess.Client;
using System;
using System.IO;
using System.Security.Cryptography;

namespace TFlexOmpFix.Procedure
{
    public static class AddFile
    {
        private static OracleCommand command;

        private static void Init()
        {
            OracleParameter p_code = new OracleParameter("p_code", OracleDbType.Decimal);
            OracleParameter p_fname = new OracleParameter("p_fname", OracleDbType.Varchar2);
            OracleParameter p_fhash = new OracleParameter("p_fhash", OracleDbType.Varchar2);
            OracleParameter p_file = new OracleParameter("p_file", OracleDbType.Blob);
            OracleParameter p_user = new OracleParameter("p_user", OracleDbType.Decimal);
            OracleParameter p_groupcode = new OracleParameter("p_groupcode", OracleDbType.Decimal);
            OracleParameter p_linkdoccode = new OracleParameter("p_linkdoccode", OracleDbType.Decimal);
            OracleParameter p_description = new OracleParameter("p_description", OracleDbType.Varchar2);
            OracleParameter p_doccode = new OracleParameter("p_doccode", OracleDbType.Decimal);

            p_doccode.Direction = System.Data.ParameterDirection.Output;

            command = new OracleCommand();
            command.Connection = Connection.GetInstance();
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.CommandText = "omp_adm.pkg_sepo_tflex_synch_omp.add_file";

            command.Parameters.AddRange(
                new OracleParameter[]
                {
                            p_code,
                            p_fname,
                            p_fhash,
                            p_file,
                            p_user,
                            p_groupcode,
                            p_linkdoccode,
                            p_description,
                            p_doccode
                });
        }

        public static void Exec(
            decimal code,
            string path,
            decimal user,
            decimal? groupcode,
            decimal? linkdoccode,
            string description,
            ref decimal doccode)
        {
            if (command == null)
            {
                Init();
            }

            var pars = command.Parameters;

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
                pars["p_linkdoccode"].Value = linkdoccode;
                pars["p_description"].Value = description;

                command.ExecuteNonQuery();

                decimal.TryParse(pars["p_doccode"].Value.ToString(), out doccode);

                stream.Close();
            }
        }

        public static bool TryExec(
            IDocLogging log,
            decimal code,
            string path,
            decimal user,
            decimal? groupcode,
            decimal? linkdoccode,
            string description,
            ref decimal doccode)
        {
            try
            {
                Exec(code, path, user, groupcode, linkdoccode, description, ref doccode);

                return true;
            }
            catch (Exception e)
            {
                log.Write(e);

                return false;
            }
        }
    }
}