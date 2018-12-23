using System;
using System.Windows.Forms;

namespace TFlexOmpFix
{
    /// <summary>
    /// Диалог с пользовательскими настройками
    /// </summary>
    public partial class SettingsDialog : Form
    {
        private ISettings iSet;

        private void Read()
        {
            Settings settings = iSet.Read();

            perfBox.Text = settings.UserName;
            sidBox.Text = settings.SID;
            loginBox.Text = settings.Login;
            passBox.Text = settings.Passw;

            //revisionBox.Checked = settings.UpdateRevision;
        }

        private void Write()
        {
            Settings settings = new Settings();

            settings.UserName = perfBox.Text;
            settings.SID = sidBox.Text;
            settings.Login = loginBox.Text;
            settings.Passw = passBox.Text;

            //settings.UpdateRevision = revisionBox.Checked;

            iSet.Write(settings);
        }

        public SettingsDialog() : this(new SettingsRegistryService())
        {
            InitializeComponent();

            Read();
        }

        public SettingsDialog(ISettings settings)
        {
            this.iSet = settings;
        }

        private void OnAccept(object sender, EventArgs e)
        {
            Write();

            DialogResult = DialogResult.OK;
        }
    }
}