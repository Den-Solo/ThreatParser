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

namespace Lab2_GUI
{
    /// <summary>
    /// Логика взаимодействия для UserMessageWnd.xaml
    /// </summary>
    public partial class UserMessageWnd : Window
    {
        public UserMessageWnd(Window parent, string content)
        {
            InitializeComponent();
            base.Owner = parent;
            parent.LocationChanged += SetPosition;
            parent.SizeChanged += SetPosition;
            parent.StateChanged += SetPosition;
            
            parent.Closed += CloseWnd;
            this.Loaded += Window_Loaded;

            this._tbContent.Text = content;

        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SetPosition(null,null);
        }
        private void SetPosition(object sender, EventArgs e)
        {
            if (base.Owner.WindowState != WindowState.Maximized)
            {
                this.Left = base.Owner.Left + base.Owner.Width - this.Width -10;
                this.Top = base.Owner.Top + base.Owner.Height - this.Height - 10;
            }
            else
            {
                var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
                this.Left = desktopWorkingArea.Right - this.Width;
                this.Top = desktopWorkingArea.Bottom - this.Height;
            }
            
        }

        private void CloseWnd(object sender, EventArgs e) => this.Close();
        private void _Exit_Click(object sender, RoutedEventArgs e) => this.Close();
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Owner?.Focus();
        }
    }
}
