using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lab2_GUI;
using Lab2_GUI.Dialogs;
using System.Net;
using System.Net.Mail;
using System.Windows;

namespace Lab2_SecurityThreatsParser
{
    static class BugReporter
    {
        private const string host = "smtp.mail.ru";
        private static readonly MailAddress to = new MailAddress("solovyov-den@list.ru", "DenSolo");
        private static readonly MailAddress from = new MailAddress("parser.threat@bk.ru", "Security threats parser");
        private const string subject = "Bug report";
        private const int timeout = 3000; //millisec
        private const int port = 25;
        private const string pswd = "^tdgS2IrTDr4";//no encryption, SeCurItY
        public static void CollectAndSend()
        {
            string report = null;
            {
                BugReportDialog dialog = new BugReportDialog("Describe the problem, please!");
                dialog.Title = "Bug reporter";
                dialog.ShowDialog();
                if (!dialog.DialogResult.HasValue || !dialog.DialogResult.Value)
                {
                    return;
                }
                report = dialog.Answer;
            }

            SmtpClient client = new SmtpClient(host, port);
            client.Timeout = timeout;
            client.EnableSsl = true;
            client.Credentials = new NetworkCredential(from.Address, pswd); 

            MailMessage msg = new MailMessage(from, to);
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
                MessageBox.Show("Something went wrong.\n" + exception, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
