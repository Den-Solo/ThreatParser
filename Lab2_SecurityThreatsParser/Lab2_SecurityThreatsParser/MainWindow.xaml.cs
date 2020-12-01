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
        public const int _MAX_PAGES = 10000;                        //value greater than possible rows.Count in table; used as "All"
        private const int _UPDATE_INTERVAL_HOUR = 0;
        private const int _UPDATE_INTERVAL_MIN = 5;
        private const int _UPDATE_INTERVAL_SEC = 0;
        public static string[] _ComboBoxPageVals { get; } = new string[5] { "15", "30", "45", "90", "All" };    //binded to combobox
        private int _tcPrevSelectedIdx = 0;                         //to avoid unnecessary combobox updates
        private UserMessageWnd _userMessageWnd = null;              //only one or zero msgWnd allowed at a moment
        private TableProcessor _table = new TableProcessor();
        private Paginator _paginatorNormal = null;
        private Paginator _paginatorChanges = null;
        private WinRegBasedTimer _timer = null;

        private int _ElementsOnPageComboBox
        {
            get
            {
                int idx = this._cbElemsOnPageCnt.SelectedIndex;
                int res = -1;
                if (idx == -1) { throw new InvalidOperationException(); }
                if (int.TryParse(_ComboBoxPageVals[idx], out res))
                {
                    return res;
                }
                else
                {
                    return _MAX_PAGES;
                }
            }
        }


        //constructors
        public MainWindow()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.DataContext = this;
            this._cbElemsOnPageCnt.SelectedIndex = 0;

            this._Prev.IsEnabled = false;
            this._Next.IsEnabled = false;
            this._tbUpdatedThreats.IsEnabled = false;

            this._dgAllThreatsList.EnableColumnVirtualization = true;
            this._dgAllThreatsList.EnableRowVirtualization = true;
            this._dgSupport.EnableColumnVirtualization = true;
            this._dgSupport.EnableRowVirtualization = true;
            this._dgChangedList.EnableColumnVirtualization = true;
            this._dgChangedList.EnableRowVirtualization = true;

            this._dgSupport.ItemsSource = new TableProcessor.FullInfo();

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
                _timer = new WinRegBasedTimer(new TimeSpan(_UPDATE_INTERVAL_HOUR, _UPDATE_INTERVAL_MIN, _UPDATE_INTERVAL_SEC), this.UpdateEventHandler, this);
            })));
        }
        private void UpdateEventHandler()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_table.CanUpdate)
                {
                    var res = MessageBox.Show(this,"Пришло время обновиться...\nХотите продолжить?", "Плановое обновление", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (res == MessageBoxResult.Yes)
                    {
                        _UpdateWeb_Click(null, null);
                    }
                }
            }));
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

        private int GetSelectedThreatId(TableProcessor.ContentMode m)
        {
            TableProcessor.ShortInfo si = null;
            if (m == TableProcessor.ContentMode.Normal)
            {
                si = (TableProcessor.ShortInfo)this._dgAllThreatsList.SelectedItem;
            }
            else
            {
                si = (TableProcessor.ShortInfo)this._dgChangedList.SelectedItem;
            }
            return int.Parse(si.Id.Substring(4));
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
        private static int ComboBoxIdxFromPaginator(ComboBox cb, Paginator p)
        {
            if (p == null)
            {
                return 0;
            }
            int idx = Array.IndexOf(_ComboBoxPageVals, p._ElementsOnPageCnt.ToString());
            if (idx != -1)
            {
                return idx;
            }
            else
            {
                return _ComboBoxPageVals.Length - 1;
            }
        }
        private TableProcessor.ContentMode ContentModeFromTcThreatsList()
        {
            return _tcThreatsList.SelectedIndex == 0 ? TableProcessor.ContentMode.Normal : TableProcessor.ContentMode.Changed;
        }
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
            string msg = "";
            var status = _table.LoadOrOpen(lm, path);
            if (status == TableProcessor.LoadStatus.OK)
            {
                if (TableProcessor.LoadModeToContentMode(lm) == TableProcessor.ContentMode.Normal)
                {
                    this._tcThreatsList.SelectedIndex = 0;
                    this._tbUpdatedThreats.IsEnabled = false;
                    _paginatorNormal = new Paginator(this._dgAllThreatsList, _table.GetShortContent(TableProcessor.ContentMode.Normal), _ElementsOnPageComboBox);
                    BtnNext_Clicked(this._dgAllThreatsList, null);

                    msg = (lm == TableProcessor.LoadMode.OpenExisting ? "Открыто успешно!" : "Загружено успешно!");
                }
                else
                {
                    _paginatorNormal = new Paginator(this._dgAllThreatsList, _table.GetShortContent(TableProcessor.ContentMode.Normal), _ElementsOnPageComboBox);
                    this._tcThreatsList.SelectedIndex = 0;
                    BtnNext_Clicked(this._dgAllThreatsList, null);

                    this._tbUpdatedThreats.IsEnabled = true;

                    _paginatorChanges = new Paginator(this._dgChangedList, _table.GetShortContent(TableProcessor.ContentMode.Changed), _ElementsOnPageComboBox);
                    this._tcThreatsList.SelectedIndex = 1;
                    BtnNext_Clicked(this._dgChangedList, null);

                    msg = "Обновлено успешно!\n";
                    msg += (_paginatorChanges._DataLength > 0 ? $"Обновленных записей: {_paginatorChanges._DataLength}" 
                        : "Вот только ничего не обновилось...\nКак часто ФСТЭК делает обновы???");
                }
            }
            else // smth went wrong - choose msg
            {
                switch (status)
                {
                    case TableProcessor.LoadStatus.NetWorkProblems:
                        msg = "Что-то пошло не так!\nСеть накрылась!";
                        break;
                    case TableProcessor.LoadStatus.FileProblems:
                        msg = "Что-то пошло не так!\nПроблемы с файлом!\nСтоит попробовать другой...";
                        break;
                    case TableProcessor.LoadStatus.SameFile:
                        msg = "Незачем открывать тот же самый файл.";
                        break;
                }
            }
            UserMsgWndShow(msg);
            return status == TableProcessor.LoadStatus.OK;
        }
        private void _UpdateWeb_Click(object sender, RoutedEventArgs e)
        {
            if (!_table.CanUpdate)
            {
                UserMsgWndShow("Нечего обновлять!");
                return;
            }
            LoadOrOpen(TableProcessor.LoadMode.DownloadUpdate, default);
        }
        private void _UpdateLocal_Click(object sender, RoutedEventArgs e)
        {
            if (!_table.CanUpdate)
            {
                UserMsgWndShow("Нечего обновлять!");
                return;
            }
            string path = FileDialogOpenShow();
            if (path != null)
            {
                LoadOrOpen(TableProcessor.LoadMode.OpenUpdate, path);
            }
        }
        private void CmdSaveAs_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (!_table.CanUpdate)
            {
                UserMsgWndShow("Нечего сохранять!");
                return;
            }
            string path = FileDialogSaveShow();
            if (path == null)
            {
                UserMsgWndShow("Не хотите, как хотите....!");
            }
            else
            {
                if (_table.SaveAs(path))
                {
                    UserMsgWndShow("Успешно сохранено!");
                }
                else
                {
                    UserMsgWndShow("Ну знаете!!!\nСохранять файл сам в себя это уже тавтология!!!");
                }
            }
        }

        private void MenuItemBugReport_Click(object sender, RoutedEventArgs e)
        {
            BugReporter.CollectAndSend();
        }

        private void BtnPrev_Clicked(object sender, RoutedEventArgs e)
        {
            Paginator p = CurPaginator();
            this._Prev.IsEnabled = p.Prev();
            this._Next.IsEnabled = p._CanGoNext;
        }
        private void BtnNext_Clicked(object sender, RoutedEventArgs e)
        {
            Paginator p = CurPaginator();
            this._Next.IsEnabled = p.Next();
            this._Prev.IsEnabled = p._CanGoPrev;
        }

        private void _cbElemsOnPageCnt_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Paginator p = CurPaginator();
            if (p != null)
            {
                p.Update(_ElementsOnPageComboBox);
                this._Prev.IsEnabled = p._CanGoPrev;
                this._Next.IsEnabled = p._CanGoNext;
            }
        }
        private void _dgAllThreatsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Update_dgSupport((DataGrid)sender);
        }
        private void _dgChangedList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Update_dgSupport((DataGrid)sender);
        }

        private void _tcThreatsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_tcPrevSelectedIdx != _tcThreatsList.SelectedIndex)
            {
                return;
            }
            else
            {
                _tcPrevSelectedIdx = 1 - _tcThreatsList.SelectedIndex;
            }

            Paginator p = CurPaginator();
            if (p != null)
            {
                this._cbElemsOnPageCnt.SelectedIndex = ComboBoxIdxFromPaginator(this._cbElemsOnPageCnt, p);
                this._Prev.IsEnabled = p._CanGoPrev;
                this._Next.IsEnabled = p._CanGoNext;
            }
        }
        private void Update_dgSupport(DataGrid sender)
        {
            if (sender.SelectedItem != null)
            {
                this._dgSupport.ItemsSource = null;
                TableProcessor.ContentMode m = ContentModeFromTcThreatsList();
                int threatId = GetSelectedThreatId(m);
                this._dgSupport.ItemsSource = _table.GetFullContent(m, threatId); ;
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
        private string FileDialogOpenShow()
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "Document"; // Default file name
            dlg.DefaultExt = ".xlsx"; // Default file extension
            dlg.Filter = "Excel files|*.xls;*.xlsx;|All|*.*"; // Filter files by extension
            var result = dlg.ShowDialog();
            if (result.HasValue && result.Value)
            {
                return dlg.FileName;
            }
            return null;
        }
        private string FileDialogSaveShow()
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "Document"; // Default file name
            dlg.DefaultExt = ".xlsx"; // Default file extension
            dlg.Filter = "Excel files|*.xlsx;*.xls;|All|*.*"; // Filter files by extension
            var result = dlg.ShowDialog();
            if (result.HasValue && result.Value)
            {
                return dlg.FileName;
            }
            return null;
        }
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e) => e.Cancel = false;
        private void MenuItemExit_Clicked(object sender, RoutedEventArgs e) => this.Close();

        private void CmdNew_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;
        private void CmdOpen_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;
        private void CmdSave_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;


    }
}
