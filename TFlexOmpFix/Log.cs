using Oracle.DataAccess.Client;
using System;
using System.IO;
using System.Text;

namespace TFlexOmpFix
{
    public class Log : ILogging
    {
        private StreamWriter log;

        public Log(string path)
        {
            log = new StreamWriter(path);
        }

        public void Write(TimeSpan span, string user, string doc, string section,
            string position, string sign, string name, decimal? qty, string doccode,
            string filePath, string error)
        {
            StringBuilder db_error = new StringBuilder();

            string timespan = string.Format("{0}:{1}:{2}:{3}",
                span.Hours, span.Minutes, span.Seconds, span.Milliseconds);

            OracleCommand command = new OracleCommand();
            command.Connection = Connection.GetLogInstance();
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.CommandText = "omp_adm.p_sepo_tflex_fixture_log";

            command.Parameters.Add("p_date", DateTime.Now);
            command.Parameters.Add("p_timespan", timespan);
            command.Parameters.Add("p_loginname", user);
            command.Parameters.Add("p_machine", System.Environment.MachineName);
            command.Parameters.Add("p_doc", System.IO.Path.GetFileName(doc));
            command.Parameters.Add("p_section", section);
            command.Parameters.Add("p_position", position);
            command.Parameters.Add("p_sign", sign);
            command.Parameters.Add("p_name", name);
            command.Parameters.Add("p_qty", qty);
            command.Parameters.Add("p_doccode", doccode);
            command.Parameters.Add("p_filepath", System.IO.Path.GetFileName(filePath));
            command.Parameters.Add("p_omptype", null);
            command.Parameters.Add("p_log", error);

            try
            {
                command.ExecuteNonQuery();
            }
            catch (Exception exc)
            {
                db_error.Append("Логирование в БД произошло некорректно! ");
                db_error.Append(exc.Message);
            }

            StringBuilder rowText = new StringBuilder();

            rowText.Append(DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
            rowText.Append(" * ");
            rowText.Append(timespan);
            rowText.Append(" * ");
            rowText.Append(user);
            rowText.Append(" * ");
            rowText.Append(section);
            rowText.Append(" * ");
            rowText.Append(position);
            rowText.Append(" * ");
            rowText.Append(sign);
            rowText.Append(" * ");
            rowText.Append(name);
            rowText.Append(" * ");
            rowText.Append(qty);
            rowText.Append(" * ");
            rowText.Append(doccode);
            rowText.Append(" * ");
            rowText.AppendLine(System.IO.Path.GetFileName(filePath));

            if (error != null)
            {
                rowText.Append("*");
                rowText.AppendLine(error);
            }

            if (db_error.ToString() != String.Empty)
            {
                rowText.AppendLine(db_error.ToString());
            }

            log.WriteLine(rowText.ToString());
        }

        public void Close()
        {
            log.Close();
        }
    }
}