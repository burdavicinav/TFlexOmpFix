using System;
using System.Windows.Forms;
using TFlex;
using TFlex.Command;
using TFlex.Model;
using TFlexOmpFix.dialogs;

namespace TFlexOmpFix
{
    public enum Commands
    {
        Export = 1,
        Settings = 2
    };

    public class OmpFixPlugin : Plugin
    {
        public OmpFixPlugin(OmpFixFactory factory) : base(factory)
        {
        }

        protected override void OnCreateTools()
        {
            //base.OnCreateTools();

            RegisterCommand(
                (int)Commands.Export,
                "Экспорт",
                TFlexOmpFix.Properties.Resources.omp_icon,
                TFlexOmpFix.Properties.Resources.omp_icon
                );

            RegisterCommand(
                (int)Commands.Settings,
                "Настройки",
                TFlexOmpFix.Properties.Resources.settings,
                TFlexOmpFix.Properties.Resources.settings
                );

            TFlex.Menu submenu = new TFlex.Menu();
            submenu.CreatePopup();
            submenu.Append((int)Commands.Export, "&Экспорт в КИС \"Омега\"", this);
            submenu.Append((int)Commands.Settings, "&Настройки экспорта", this);

            TFlex.Application.ActiveMainWindow.InsertPluginSubMenu(
                "Экспорт",
                submenu,
                MainWindow.InsertMenuPosition.PluginSamples,
                this
                );

            int[] cmdIDs = new int[] { (int)Commands.Export, (int)Commands.Settings };
            CreateToolbar("Экспорт", cmdIDs);
            CreateMainBarPanel("Экспорт", cmdIDs, this.ID, true);

            TFlex.RibbonGroup ribbonGroup = TFlex.RibbonBar.ApplicationsTab.AddGroup("Экспорт");
            ribbonGroup.AddButton((int)Commands.Export, this);
            ribbonGroup.AddButton((int)Commands.Settings, this);

            //MessageBox.Show("Test!!!");

            if (TFlex.Application.ActiveDocument != null)
                TFlex.Application.ActiveDocument.AttachPlugin(this);
        }

        protected override void OnCommand(Document document, int id)
        {
            switch ((Commands)id)
            {
                case Commands.Export:

                    // Активный документ
                    Document doc = TFlex.Application.ActiveDocument;

                    // логирование
                    ILogging iLog = new Log(doc.FilePath + "\\ompexp.txt");

                    Settings settings = new Settings();

                    try
                    {
                        // настройки
                        ISettings iSettings = new SettingsRegistryService();

                        settings = iSettings.Read();

                        // подключение к БД
                        Connection.SetInstance(settings);

                        FixtureTypeDialog dlg = new FixtureTypeDialog();

                        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            // транзакция
                            Connection.OpenTransaction();

                            // экспорт
                            FixtureOmpLoad mngr = new FixtureOmpLoad(settings, iLog, doc, dlg.FixType);
                            mngr.Export();

                            // применение изменений
                            Connection.Commit();

                            MessageBox.Show(
                                "Экспорт успешно завершен!",
                                "Информация",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }
                    }
                    catch (Exception e)
                    {
                        Connection.Rollback();

                        MessageBox.Show(
                            e.Message,
                            "Ошибка!",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);

                        iLog.Write(new TimeSpan(), settings.UserName, doc.FileName, null, null, null, null,
                            null, null, null, e.Message + " * " + e.StackTrace);
                    }
                    finally
                    {
                        Connection.Close();
                        iLog.Close();
                    }

                    break;

                case Commands.Settings:

                    SettingsDialog dialog = new SettingsDialog();
                    dialog.ShowDialog();

                    break;

                default:
                    base.OnCommand(document, id);
                    break;
            }
        }

        protected override void OnUpdateCommand(CommandUI cmdUI)
        {
            if (cmdUI == null)
                return;

            if (cmdUI.Document == null)
            {
                cmdUI.Enable(false);
                return;
            }

            cmdUI.Enable();
        }
    };
}