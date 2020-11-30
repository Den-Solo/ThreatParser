using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Microsoft.Office.Interop.Excel; //additional dependancy on MS Office installed but ok
using System.IO;
using System.Windows;


namespace Lab2_SecurityThreatsParser
{
    public partial class TableProcessor : IDisposable
    {

        private const string defaultUri_ = @"https://bdu.fstec.ru/files/documents/thrlist.xlsx";
        private const string defaultTableName_ = "thrlist.xlsx";
        private string currentTablePath_ = null;
        private string currentTableName_ = null;
        public bool IsValid { get; private set; } = false;
        private string[] headers_;
        private ThreatContent[] tableContent_;

        private Microsoft.Office.Interop.Excel.Application xlApp_ = null;
        private Microsoft.Office.Interop.Excel.Workbook xlWorkBook_ = null;
        private Microsoft.Office.Interop.Excel.Worksheet xlWorkSheet_ = null;
        private Microsoft.Office.Interop.Excel.Range xlRange_ = null;

        public TableProcessor() { }

        public void Dispose()
        {
            if (xlWorkBook_ != null)
            {
                xlWorkBook_.Close();
                xlWorkBook_ = null;
            }
            if (xlApp_ != null)
            {
                xlApp_.Quit();
                xlApp_ = null;
            }
            xlRange_ = null;
            xlWorkSheet_ = null;
        }
        ~TableProcessor()
        {
            Dispose();
        }

 
        public bool LoadNewTableWebDefault()
        {
            Dispose();
            currentTableName_ = defaultTableName_;
            currentTablePath_ = Directory.GetCurrentDirectory() + "\\" + defaultTableName_;
            try
            {
                LoadNewTableWeb(TableProcessor.defaultUri_,defaultTableName_);
                OpenExcelTable();
                ReadTableAll();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                this.IsValid = false;
                return false;
            }
            finally
            {
                Dispose();
            }
            return true;
        }
        public void OpenExcelTable() // currentTablePath_ must be set correctly before
        {
           Dispose();
           xlApp_ = new Microsoft.Office.Interop.Excel.Application();
           xlWorkBook_ = xlApp_.Workbooks.Open(currentTablePath_);
           xlWorkSheet_ = xlWorkBook_.Sheets[1];
           xlRange_ = xlWorkSheet_.UsedRange;
        }
        private void ReadTableAll()
        {
            if (xlRange_ == null)
            {
                throw new InvalidOperationException();
            }
            ReadHeader();
            ReadAllLines();
        }
        private void ReadHeader()
        {
            int colCnt = xlRange_.Columns.Count;
            const int rowIdx = 2;

            headers_ = new string[colCnt];
            for (int i = 1; i <= colCnt; ++i)
            {
                if (xlRange_.Cells[rowIdx, i] != null && xlRange_.Cells[rowIdx, i].Value2 != null) 
                {
                    xlRange_.Rows[rowIdx].
                    headers_[i - 1] = xlRange_.Cells[rowIdx, i].Value2.ToString();
                }
            }
        }
        private void ReadAllLines()
        {
            int rowCnt = xlRange_.Rows.Count;
            int colCnt = xlRange_.Columns.Count;
            const int rowIdxFirst = 3;
            tableContent_ = new ThreatContent[rowCnt - 2];
            for (int i = rowIdxFirst; i <= rowCnt; ++i)
            {
                tableContent_[i - rowIdxFirst] = new ThreatContent(colCnt);
                for (int j = 1; j <= colCnt; ++j)
                {
                    if (xlRange_.Cells[i, j] != null && xlRange_.Cells[i,j].Value2 != null)
                    {
                        tableContent_[i - rowIdxFirst].content[j - 1] = xlRange_.Cells[i, j].Value2.ToString();
                    }
                }
                
            }
        }
        public void OpenLocalTable(string path) //null or empty path stands for default path
        {
            Dispose();
            if (string.IsNullOrEmpty(path))
            {
                path = Directory.GetCurrentDirectory() + "\\" + defaultTableName_;
            }
            currentTablePath_ = path;
            currentTableName_ = path.Split('\\').Last();
            OpenExcelTable();
        }
        public void LoadNewTableWebDefaultAsync()
        {
            Dispose();
            LoadNewTableWeb(TableProcessor.defaultUri_,defaultTableName_);
        }
        private static void LoadNewTableWeb(string uri,string pathToSave)
        {
            WebClient webClient = new WebClient();
            webClient.DownloadFile(uri, pathToSave);
        }


        public ShortInfo[] GetShortContentAll()
        {
            if (tableContent_ == null)
            {
                throw new InvalidOperationException();
            }

            ShortInfo[] result = new ShortInfo[tableContent_.Length];

            for (int i = 0; i < tableContent_.Length; ++i)
            {
               result[i] = new ShortInfo("УБИ." + tableContent_[i].content[0].PadLeft(3,'0'), tableContent_[i].content[1]);
            }
            return result;
        }
        public FullInfo GetFullContent(int threatID)
        {
            return new FullInfo(headers_.Zip(tableContent_[threatID - 1].content, (x, y) => new Tuple<string, string>(x, y)).ToArray());
        }

        public void GetUpdatedRaws(string path)
        {

        }
    }
}
