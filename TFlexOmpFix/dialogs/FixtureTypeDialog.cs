using Oracle.DataAccess.Client;
using System.Collections.Generic;
using System.Windows.Forms;
using TFlexOmpFix.Objects;

namespace TFlexOmpFix.dialogs
{
    public partial class FixtureTypeDialog : Form
    {
        public int FixType { get; private set; }

        public FixtureTypeDialog()
        {
            InitializeComponent();

            OracleCommand command = new OracleCommand();
            command.Connection = Connection.GetInstance();
            command.CommandText = "select code, name from omp_adm.fixture_types order by name";

            Dictionary<int, string> dict = new Dictionary<int, string>();

            using (OracleDataReader rd = command.ExecuteReader())
            {
                while (rd.Read())
                {
                    FIXTURE_TYPES fixType = new FIXTURE_TYPES
                    {
                        CODE = rd.GetInt32(0),
                        NAME = rd.GetString(1)
                    };

                    dict.Add(fixType.CODE, fixType.NAME);
                }
            }

            fixTypeBox.DisplayMember = "Value";
            fixTypeBox.ValueMember = "Key";
            fixTypeBox.DataSource = new BindingSource(dict, null);

            okButton.Click += OkButton_Click;
        }

        private void OkButton_Click(object sender, System.EventArgs e)
        {
            FixType = (int)fixTypeBox.SelectedValue;
        }
    }
}