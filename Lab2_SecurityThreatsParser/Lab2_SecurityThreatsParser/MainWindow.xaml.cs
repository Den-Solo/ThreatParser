using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Net.Mail;
using Lab2_GUI.Dialogs;
using Lab2_SecurityThreatsParser;
using System.Collections.ObjectModel;

namespace Lab2_GUI
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool isDataSaved = true;
        private TableProcessor table_ = new TableProcessor();

        public ObservableCollection<TableProcessor.ShortInfo> Si { get; set; } = new ObservableCollection<TableProcessor.ShortInfo>();
        public ObservableCollection<Tuple<string, string>> Fi { get; set; } = new ObservableCollection<Tuple<string, string>>();
        public MainWindow()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            this.MainDataGrid.ItemsSource = Si;
            this.SupportDataGrid.ItemsSource = Fi;
        }

        void Save() { }
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            string msg = "";
            string title = "Close app";
            MessageBoxResult result = default;

            if (this.isDataSaved)
            {
                msg = "Are you sure?";
                result = WPFCustomMessageBox.CustomMessageBox.ShowOKCancel(msg, title, "Yes", "No",MessageBoxImage.Exclamation);
            }
            else
            {
                msg = "You have unsaved changes?";
                result = WPFCustomMessageBox.CustomMessageBox.ShowYesNoCancel(msg, title, "Save", "Don't save", "Cancel",MessageBoxImage.Exclamation);
                if (result == MessageBoxResult.OK)
                {
                    Save();
                }
            }
            if (result == MessageBoxResult.Cancel || result == MessageBoxResult.None)
            {
                e.Cancel = true;
            }
        }

        private void MenuItemExit_Clicked(object sender, RoutedEventArgs e)
        {
            //Application.Current.Shutdown();
            this.Close();
        }

   
  

        private void CommandBindingNew_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;
        private void CommandBindingOpen_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;
        private void CommandBindingUndo_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;
        private void CommandBindingRedo_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;
        private void CommandBindingSave_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;

        private void CommandBindingNew_Executed(object sender, ExecutedRoutedEventArgs e)
        {

            table_.LoadNewTableWebDefault();
            TableProcessor.ShortInfo[] rows = table_.GetShortContentInRange(3,10);
            Si.Clear();
            
            
            if (rows != null)
            {
                foreach (var note in rows)
                {
                    //this.MainDataGrid.Items.Add(note);
                    Si.Add(note);
                }
            }
            else
            {
                MessageBox.Show("Строк нет", "Оишбка");
            }
        }
        private void CommandBindingOpen_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            
            MessageBox.Show("Open", "", MessageBoxButton.OK);
        }
        private void CommandBindingUndo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            
            MessageBox.Show("Undo", "", MessageBoxButton.OK);
        }
        private void CommandBindingRedo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            
            MessageBox.Show("Redo", "", MessageBoxButton.OK);
        }
        private void CommandBindingSave_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            
            MessageBox.Show("Save", "", MessageBoxButton.OK);
        }

        private void MenuItemAbout_Click(object sender, RoutedEventArgs e)
        {
            const string content = "Designed by [DATA EXPUNGED].\nPowered by FirstLineSoftware educational initiative.";
            var result = WPFCustomMessageBox.CustomMessageBox.ShowOKCancel(content, "About", "Learn More","Cancel",MessageBoxImage.Information);
            if (result == MessageBoxResult.OK)
            {
                System.Diagnostics.Process.Start("https://github.com/Den-Solo/CSharp-FirstLineSoftware-Course");
            }
        }
        private void Row_Selected(object sender, RoutedEventArgs e)
        {
            int threatId = int.Parse(((TableProcessor.ShortInfo)this.MainDataGrid.Items.GetItemAt(this.MainDataGrid.SelectedIndex)).Id);
            var res = table_.GetFullContent(threatId);
            Fi.Clear();
            
            foreach (var pair in res)
            {
                Fi.Add(pair);
              //  this.SupportDataGrid.Items.Add(pair);
            }
        }

        private void MenuItemBugReport_Click(object sender, RoutedEventArgs e)
        {
            string report = null;
            {
                BugReportDialog dialog = new BugReportDialog("Describe the problem, please!");
                dialog.Title = "Bug reporter";
                dialog.ShowDialog();
                if (! dialog.DialogResult.HasValue || !dialog.DialogResult.Value)
                {
                    return;
                }
                report = dialog.Answer;
            }
            string host = "smtp.mail.ru";
            MailAddress to = new MailAddress("solovyov-den@list.ru","DenSolo");
            MailAddress from = new MailAddress("parser.threat@bk.ru","Security threats parser");
            string subject = "Bug report";

            SmtpClient client = new SmtpClient(host, 25);
            client.Timeout = 3_000;
            client.EnableSsl = true;
            client.Credentials = new NetworkCredential(from.Address, "^tdgS2IrTDr4"); //no encryption, SeCurItY

            MailMessage msg = new MailMessage(from,to);
            msg.Subject = subject;
            msg.Body = report;
            
            try
            {
                // client.SendAsync(from,to, subject,report,callBack);
                client.Send(msg);
                MessageBox.Show("Report has been sent succesfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception exception)
            {
                MessageBox.Show("Something went wrong.\n"+exception, "Error",MessageBoxButton.OK,MessageBoxImage.Error);
            }
            
        }


    }
}
