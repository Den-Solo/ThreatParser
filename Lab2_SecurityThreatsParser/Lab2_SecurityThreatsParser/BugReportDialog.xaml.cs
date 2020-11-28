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
using System.Windows.Shapes;

namespace Lab2_GUI.Dialogs
{
	public partial class BugReportDialog : Window
	{
		public BugReportDialog(string question, string defaultAnswer = "")
		{
			InitializeComponent();
			lblQuestion.Content = question;
			txtAnswer.Text = defaultAnswer;
			this.btnDialogOk.IsEnabled = false;
		}

		private void btnDialogOk_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;
		}


		public string Answer
		{
			get { return txtAnswer.Text; }
		}

        private void txtAnswer_TextChanged(object sender, TextChangedEventArgs e)
        {
			if (this.btnDialogOk != null)
			{
				this.btnDialogOk.IsEnabled = (this.txtAnswer.Text.Length > 0);
			}
		}
    }
}
