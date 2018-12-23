using Oracle.DataAccess.Client;
using System.Text;

namespace TFlexOmpFix
{
    public class OracleProcedure
    {
        public string Name { get; set; }

        public string Package { get; set; }

        public string Scheme { get; set; }

        public string FullName
        {
            get
            {
                StringBuilder fullname = new StringBuilder();

                if (Scheme != null)
                {
                    fullname.Append(Scheme);
                    fullname.Append(".");
                }

                if (Package != null)
                {
                    fullname.Append(Package);
                    fullname.Append(".");
                }

                fullname.Append(Name);

                return fullname.ToString();
            }
        }

        protected OracleCommand command;
    }
}