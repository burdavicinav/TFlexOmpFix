using Oracle.DataAccess.Client;
using System;
using System.IO;
using System.Text;

namespace TFlexOmpFix
{
    public class Log : IDocLogging
    {
        private StreamWriter log;

        public TimeSpan Span { get; set; }

        public string User { get; set; }

        public string Document { get; set; }

        public string Section { get; set; }

        public string Position { get; set; }

        public string Sign { get; set; }

        public string Name { get; set; }

        public decimal? Qty { get; set; }

        public string Doccode { get; set; }

        public string FilePath { get; set; }

        public string Error { get; set; }

        public Log(string path)
        {
            log = new StreamWriter(path);
        }

        public void Write()
        {
            StringBuilder db_error = new StringBuilder();

            string timespan = string.Format("{0}:{1}:{2}:{3}",
                Span.Hours, Span.Minutes, Span.Seconds, Span.Milliseconds);

            OracleCommand command = new OracleCommand();
            command.Connection = Connection.GetLogInstance();
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.CommandText = "omp_adm.p_sepo_tflex_fixture_log";

            command.Parameters.Add("p_date", DateTime.Now);
            command.Parameters.Add("p_timespan", timespan);
            command.Parameters.Add("p_loginname", User);
            command.Parameters.Add("p_machine", System.Environment.MachineName);
            command.Parameters.Add("p_doc", System.IO.Path.GetFileName(Document));
            command.Parameters.Add("p_section", Section);
            command.Parameters.Add("p_position", Position);
            command.Parameters.Add("p_sign", Sign);
            command.Parameters.Add("p_name", Name);
            command.Parameters.Add("p_qty", Qty);
            command.Parameters.Add("p_doccode", Doccode);
            command.Parameters.Add("p_filepath", System.IO.Path.GetFileName(FilePath));
            command.Parameters.Add("p_omptype", null);
            command.Parameters.Add("p_log", Error);

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
            rowText.Append(User);
            rowText.Append(" * ");
            rowText.Append(Section);
            rowText.Append(" * ");
            rowText.Append(Position);
            rowText.Append(" * ");
            rowText.Append(Sign);
            rowText.Append(" * ");
            rowText.Append(Name);
            rowText.Append(" * ");
            rowText.Append(Qty);
            rowText.Append(" * ");
            rowText.Append(Doccode);
            rowText.Append(" * ");
            rowText.AppendLine(System.IO.Path.GetFileName(FilePath));

            if (Error != null)
            {
                rowText.Append("*");
                rowText.AppendLine(Error);
            }

            if (db_error.ToString() != String.Empty)
            {
                rowText.AppendLine(db_error.ToString());
            }

            log.WriteLine(rowText.ToString());
        }

        public void Write(Exception e)
        {
            Error = e.Message;

            Write();
        }

        public void Close()
        {
            log.Close();
        }
    }
}