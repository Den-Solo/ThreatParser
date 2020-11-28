using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Microsoft.Office.Interop.Excel;
using System.IO;
using System.Windows;
namespace Lab2_SecurityThreatsParser
{
    public class TableProcessor : IDisposable
    {
        public class ShortInfo 
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public ShortInfo(string id, string name)
            {
                Id = id;
                Name = name;
            }
        }
 

        private const string defaultUri_ = @"https://bdu.fstec.ru/files/documents/thrlist.xlsx";
        private const string defaultTableName_ = "thrlist.xlsx";
        private string currentTablePath_ = null;
        private string currentTableName_ = null;

        private Microsoft.Office.Interop.Excel.Application xlApp_ = null;
        private Microsoft.Office.Interop.Excel.Workbook xlWorkBook_ = null;
        private Microsoft.Office.Interop.Excel.Worksheet xlWorkSheet_ = null;
        private Microsoft.Office.Interop.Excel.Range xlRange_ = null;

        public TableProcessor() { }

        public void Dispose()
        {
            CloseTable();
        }
        ~TableProcessor()
        {
            Dispose();
        }

        public void CloseTable()
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
        public void LoadNewTableWebDefault()
        {
            CloseTable();

            currentTableName_ = defaultTableName_;
            currentTablePath_ = Directory.GetCurrentDirectory() + "\\" + defaultTableName_;

            try
            {
                LoadNewTableWeb(TableProcessor.defaultUri_,defaultTableName_);

                xlApp_ = new Microsoft.Office.Interop.Excel.Application();
                xlWorkBook_ = xlApp_.Workbooks.Open(currentTablePath_);
                xlWorkSheet_ = xlWorkBook_.Sheets[1];
                xlRange_ = xlWorkSheet_.UsedRange;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                CloseTable();
            }
        }
        public void LoadNewTableWebDefaultAsync()
        {
            LoadNewTableWeb(TableProcessor.defaultUri_,defaultTableName_);
        }
        private static void LoadNewTableWeb(string uri,string pathToSave)
        {
            WebClient webClient = new WebClient();
            webClient.DownloadFile(uri, pathToSave); // relative address used because there is no need to leave trash in %USER% directory
        }
       // public void LoadNewTableWebAsync(string uri)
       // {
       //     WebClient webClient = new WebClient();
       //     webClient.DownloadFile(uri, defaultTableName_); // relative address used because there is no need to leave trash in %USER% directory
       //
       // }

        public ShortInfo[] GetShortContentInRange(int rowIdxBeg, int count)
        {
            if (xlRange_ == null)
            {
                return null;
            }
            if (rowIdxBeg < 2)
            {
                rowIdxBeg = 2;
            }
            int rowIdxLast = rowIdxBeg + count;
            rowIdxLast = (rowIdxLast >= xlRange_.Rows.Count ? xlRange_.Rows.Count : rowIdxLast);
            ShortInfo[] result = new ShortInfo[rowIdxLast - rowIdxBeg];

            for (int i = rowIdxBeg; i < rowIdxLast; ++i)
            {
                if (xlRange_.Cells[i, 1] != null && xlRange_.Cells[i, 2] != null && xlRange_.Cells[i, 1].Value2 != null && xlRange_.Cells[i, 2].Value2 != null )
                {
                    result[i - rowIdxBeg] = new ShortInfo(xlRange_.Cells[i, 1].Value2.ToString(), xlRange_.Cells[i, 2].Value2.ToString());
                }
            }
            return result;
        }
        public Tuple<string,string>[] GetFullContent(int threatID)
        {
            int MAX_THREAT_CNT = xlRange_.Columns.Count;
            const int HEADER_ROW_IDX = 2;
            if (threatID >= xlRange_.Rows.Count)
            {
                return null;
            }
            Tuple<string, string>[] result = new Tuple<string, string>[MAX_THREAT_CNT];
            for (int i = 1; i <= MAX_THREAT_CNT; ++i)
            {
                result[i] = new Tuple<string, string>(xlRange_.Cells[HEADER_ROW_IDX, i].Value2.ToString(), xlRange_.Cells[threatID, i].Value2.ToString());
            }
            return result;
        }

        public void GetUpdatedRaws(string path)
        {

        }
    }
}
