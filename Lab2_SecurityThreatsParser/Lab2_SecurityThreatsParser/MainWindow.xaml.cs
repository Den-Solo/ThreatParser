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
using System.Threading;

namespace Lab2_GUI
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml: У самурая нет логики, только путь
    /// </summary>
    public partial class MainWindow : Window
    {
        //private
        public const int _MAX_PAGES = 10000;
        public static string[] _ComboBoxPageVals {  get; } = new string[5] { "15", "30", "45", "90", "All" };
        private int _tcPrevSelectedIdx = 0;
        private UserMessageWnd _userMessageWnd = null;
        private TableProcessor _table = new TableProcessor();
        private Paginator _paginatorNormal = null;
        private Paginator _paginatorChanges = null;

        private int _ElementsOnPageComboBox
        {
            get
            {
                int idx = this._cbElemsOnPageCnt.SelectedIndex;
                int res = -1;
                if (idx == -1)
                {
                    throw new InvalidOperationException();
                }
                if (int.TryParse(_ComboBoxPageVals[idx],out res))
                {
                    return res;
                }
                else
                {
                    return _MAX_PAGES;
                }
            }
        }
        private int _CurSelectedThreatIdAll
        {
            get
            {
                TableProcessor.ShortInfo si = (TableProcessor.ShortInfo)this.dgAllThreatsList.SelectedItem;
                if (si == null)
                {
                    return 0;
                }
                return int.Parse(si.Id.Substring(4));
            }
        }
        private int _CurSelectedThreatIdChanged
        {
            get
            {
                TableProcessor.ShortInfo si = (TableProcessor.ShortInfo)this.dgChangesList.SelectedItem;
                return int.Parse(si.Id.Substring(4));
            }
        }
        private TableProcessor.FullInfo fullInfoList = null;

        //constructors
        public MainWindow()
        { 
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.DataContext = this;
            this._cbElemsOnPageCnt.SelectedIndex = 0;

            this._Prev.IsEnabled = false;
            this._Next.IsEnabled = false;
            this.tbUpdatedThreats.IsEnabled = false;

            this.dgAllThreatsList.EnableColumnVirtualization = true;
            this.dgAllThreatsList.EnableRowVirtualization = true;
            this.dgAllThreatsList.MaxHeight = 5000;
            this.dgAllThreatsList.MaxWidth = 5000;
            this.dgSupport.EnableColumnVirtualization = true;
            this.dgSupport.EnableRowVirtualization = true;
            this.dgChangesList.EnableColumnVirtualization = true;
            this.dgChangesList.EnableRowVirtualization = true;

            this.MainGrid.Focus();
            Loaded += MainWindow_Loaded;
        }

        //methods (event handlers)
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            Dispatcher.BeginInvoke(new Action(() =>
            {
                OpenOnStart();
            })));
        }
        private void OpenOnStart()
        {
            if (!LoadOrOpen(TableProcessor.LoadMode.OpenExisting, default))
            {
                var res = MessageBox.Show("Локальная БД не обнаружена по пути: " + _table.CurrentTablePath
                    + ".\nЖелаете выполнить загрузку из сети?", "Ошибка", MessageBoxButton.YesNo, MessageBoxImage.Error);
                if (res == MessageBoxResult.Yes)
                {
                    if (!LoadOrOpen(TableProcessor.LoadMode.DownloadNew, default))
                    {
                        MessageBox.Show("Не удалось выполнить загрузку из сети!" +
                            "\nПроверьте подключение или обратитесь к своему системному администратору.",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            _userMessageWnd?.Close();
        }

        void Save() { }


        private void CmdNew_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            LoadOrOpen(TableProcessor.LoadMode.DownloadNew, default);
        }
        private void CmdOpen_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            LoadOrOpen(TableProcessor.LoadMode.OpenExisting, default);
        }
        private bool LoadOrOpen(TableProcessor.LoadMode lm, string path)
        {
            if (!_table.LoadOrOpen(lm, path))
            {
                UserMsgWndShow("Что-то пошло не так!");
                return false;
            }
            if (TableProcessor.LoadModeToContentMode(lm) == TableProcessor.ContentMode.Normal)
            {
                this._tcThreatsList.SelectedIndex = 0;
                this.tbUpdatedThreats.IsEnabled = false;
                _paginatorNormal = new Paginator(this.dgAllThreatsList, _table.GetShortContent(TableProcessor.ContentMode.Normal), _ElementsOnPageComboBox);
                this._Next.IsEnabled = _paginatorNormal.Next();
                this._Prev.IsEnabled = _paginatorNormal.CanGoPrev;
            }
            else
            {
                this._tcThreatsList.SelectedIndex = 1;
                this.tbUpdatedThreats.IsEnabled = true;
                _paginatorChanges = new Paginator(this.dgChangesList, _table.GetShortContent(TableProcessor.ContentMode.Changed), _ElementsOnPageComboBox);
                this._Next.IsEnabled = _paginatorChanges.Next();
                this._Prev.IsEnabled = _paginatorChanges.CanGoPrev;
            }

            if (TableProcessor.LoadModeToContentMode(lm) == TableProcessor.ContentMode.Normal) 
            {
                UserMsgWndShow((lm == TableProcessor.LoadMode.OpenExisting ? "Открыто успешно!" : "Загружено успешно!"));
            }
            else
            {
                UserMsgWndShow("Обновлено успешно!");
            }
            return true;
        }
        private void _UpdateWeb_Click(object sender, RoutedEventArgs e)
        {
            LoadOrOpen(TableProcessor.LoadMode.DownloadUpdate, default);
            this.tbUpdatedThreats.IsEnabled = true;
        }
        private void _UpdateLocal_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "Document"; // Default file name
            dlg.DefaultExt = ".txt"; // Default file extension
            dlg.Filter = "Excel files|*.xls;*.xlsx;|All|*.*"; // Filter files by extension
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                LoadOrOpen(TableProcessor.LoadMode.OpenUpdate, dlg.FileName);
                this.tbUpdatedThreats.IsEnabled = true;
            }
        }
        private void CmdSave_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            MessageBox.Show("Save", "", MessageBoxButton.OK);
        }

        private void MenuItemBugReport_Click(object sender, RoutedEventArgs e)
        {
            BugReporter.CollectAndSend();
        }
        private ref Paginator CurPaginator()
        {
            if (this._tcThreatsList.SelectedIndex == 0)
            {
                return ref _paginatorNormal;
            }
            else
            {
                return ref _paginatorChanges;
            }
        }
        private ref Paginator GetPaginator(TableProcessor.ContentMode cm)
        {
            if (cm == TableProcessor.ContentMode.Normal)
            {
                return ref _paginatorNormal;
            }
            else
            {
                return ref _paginatorChanges;
            }
        }
        private void BtnPrev_Clicked(object sender, RoutedEventArgs e)
        {
            Paginator p = CurPaginator();
            this._Prev.IsEnabled = p.Prev();
            this._Next.IsEnabled = p.CanGoNext;
        }
        private void BtnNext_Clicked(object sender, RoutedEventArgs e)
        {
            Paginator p = CurPaginator();
            this._Next.IsEnabled = p.Next();
            this._Prev.IsEnabled = p.CanGoPrev;
        }

        private void _cbElemsOnPageCnt_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Paginator p = CurPaginator();
            if (p != null)
            {
                p.Update(_ElementsOnPageComboBox);
                this._Prev.IsEnabled = p.CanGoPrev;
                this._Next.IsEnabled = p.CanGoNext;
            }
        }
        private void dgAllThreatsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateDgSupport((DataGrid)sender);
        }
        private void dgUpdatedList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateDgSupport((DataGrid)sender);
        }
        private static int ComboBoxIdxFromPaginator(ComboBox cb,Paginator p)
        {
            if (p == null)
            {
                return 0;
            }
            int idx = Array.IndexOf(_ComboBoxPageVals, p.CurElementsOnPageCnt_.ToString());
            if (idx != -1)
            {
                return idx;
            }
            else
            {
                return _ComboBoxPageVals.Length - 1;
            }
        }
        private void _tcThreatsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_tcPrevSelectedIdx == _tcThreatsList.SelectedIndex)
            {
                return;
            }
            else
            {
                _tcPrevSelectedIdx = 1 - _tcThreatsList.SelectedIndex;
            }

            Paginator p = null;
            if (_tcThreatsList.SelectedIndex == 0)
            {
                p = _paginatorNormal;
            }
            else
            {
                p = _paginatorChanges;
            }
            if (p != null)
            {
                this._cbElemsOnPageCnt.SelectedIndex = ComboBoxIdxFromPaginator(this._cbElemsOnPageCnt, p);
                this._Prev.IsEnabled = p.CanGoPrev;
                this._Next.IsEnabled = p.CanGoNext;
            }
        }
        private void UpdateDgSupport(DataGrid sender)
        {
            if (sender.SelectedItem != null)
            {
                this.dgSupport.ItemsSource = null;
                TableProcessor.ContentMode m = TableProcessor.ContentMode.Normal;
                int threatId = _CurSelectedThreatIdAll;
                if (this._tcThreatsList.SelectedIndex == 1)
                {
                    m = TableProcessor.ContentMode.Changed;
                    threatId = _CurSelectedThreatIdChanged;
                }
                fullInfoList = _table.GetFullContent(m, threatId);
                this.dgSupport.ItemsSource = fullInfoList;
            }
        }
        private void MenuItemAbout_Click(object sender, RoutedEventArgs e)
        {
            const string content = "Designed by [DATA EXPUNGED].\nPowered by FirstLineSoftware educational initiative.";
            var result = WPFCustomMessageBox.CustomMessageBox.ShowOKCancel(content, "About", "Learn More", "Cancel", MessageBoxImage.Information);
            if (result == MessageBoxResult.OK)
            {
                System.Diagnostics.Process.Start("https://github.com/Den-Solo/CSharp-FirstLineSoftware-Course");
            }
        }
        private void UserMsgWndShow(string content)
        {
            _userMessageWnd?.Close();
            _userMessageWnd = new UserMessageWnd(this, content);
            _userMessageWnd.Show();
        }
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e) => e.Cancel = false;
        private void MenuItemExit_Clicked(object sender, RoutedEventArgs e) => this.Close();

        private void CmdNew_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;
        private void CmdOpen_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;
        private void CmdSave_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;


    }
}
