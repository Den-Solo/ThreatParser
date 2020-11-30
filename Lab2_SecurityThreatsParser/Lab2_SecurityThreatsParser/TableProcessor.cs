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
using System.Data;
using ExcelDataReader;

namespace Lab2_SecurityThreatsParser
{
    public partial class TableProcessor/* : IDisposable*/
    {
        const string YES = "Да";
        const string NO = "Нет";
        private const string _defaultUri = @"https://bdu.fstec.ru/files/documents/thrlist.xlsx";
        private const string _defaultTableName = "thrlist.xlsx";
        private readonly static string _defaultTablePath = Directory.GetCurrentDirectory() + "\\" + _defaultTableName;
        public string CurrentTablePath { get; private set; } = null;
        public string CurrentTableName { get; private set; } = null;
        public bool IsUpdated { get; private set; } = false;
        private string[] _headers;
        private ThreatContent[] _tableContentNormal;
        private ThreatContent[] _tableContentChanged;

        private DataSet tableDataSet_ = null;

        public enum LoadMode 
        {
            DownloadNew,
            OpenExisting,
            DownloadUpdate,
            OpenUpdate
        }
        public enum ContentMode
        {
            Normal,     //values displayed in main grid - all threats
            Changed     //threats which were changed by updated vals
        }

        public static ContentMode LoadModeToContentMode(LoadMode lm)
        {
            if (lm == LoadMode.DownloadNew || lm == LoadMode.OpenExisting)
            {
                return ContentMode.Normal;
            }
            else
            {
                return ContentMode.Changed;
            }
        }
        public bool LoadOrOpen(LoadMode lm, string localPath)
        {
            if (lm == LoadMode.DownloadUpdate || lm == LoadMode.OpenUpdate)
            {
                File.Delete(CurrentTablePath + ".deprecated");
                File.Move(CurrentTablePath, CurrentTablePath + ".deprecated");
                if (!string.IsNullOrEmpty(localPath) && localPath != _defaultTablePath)
                {
                    File.Copy(localPath, _defaultTablePath);
                    localPath = null;
                }
            }
            if (string.IsNullOrWhiteSpace(localPath))
            {
                CurrentTableName = _defaultTableName;
                CurrentTablePath = _defaultTablePath;
            }
            else //in current program state it never gets here
            {
                CurrentTableName = localPath.Split('\\').Last();
                CurrentTablePath = localPath;
            }
            try
            {
                if (lm == LoadMode.DownloadNew || lm == LoadMode.DownloadUpdate)
                {
                    LoadNewTableWeb(TableProcessor._defaultUri, CurrentTableName);
                }
                OpenExcelTable();
                ReadTableAll(LoadModeToContentMode(lm));
                tableDataSet_ = null;
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
        public void OpenExcelTable() // CurrentTablePath must be set correctly before
        {
            using (var fs = File.OpenRead(CurrentTablePath)) 
            {
                using (var reader = ExcelReaderFactory.CreateReader(fs))
                {
                    tableDataSet_ = reader.AsDataSet();
                }
            }
        }
        private void ReadTableAll(ContentMode m)
        {
            if (m == ContentMode.Normal)
            {
                ReadHeader(tableDataSet_,ref _headers);
                ReadAllLines(tableDataSet_,ref  _tableContentNormal);
            }
            else
            {
                _tableContentChanged = _tableContentNormal;
                ReadAllLines(tableDataSet_,ref _tableContentNormal);
                _tableContentChanged = GetUpdatedThreats(_tableContentNormal, _tableContentChanged);
            }
            
        }
        private static void ReadHeader(in DataSet src,ref string[] dest)
        {
            dest = src.Tables[0].Rows[1].ItemArray.Select(s => s.ToString()).ToArray();
        }
        private static void ReadAllLines(in DataSet src,ref ThreatContent[] dest)
        {
            int rowCnt = src.Tables[0].Rows.Count;
            const int rowIdxFirst = 2;
            dest = new ThreatContent[rowCnt - rowIdxFirst];
            for (int i = rowIdxFirst; i < rowCnt; ++i)
            {
                dest[i - rowIdxFirst] = new ThreatContent(src.Tables[0].Rows[i].ItemArray.Select(s => s.ToString()).ToArray());
                for (int j = 5; j < 8 && j < dest[i - rowIdxFirst].content.Length; ++j)
                {
                    dest[i - rowIdxFirst].content[j] = (dest[i - rowIdxFirst].content[j] == "1" ? YES : NO);
                }
            }
        }
        private static ThreatContent[] GetUpdatedThreats(in ThreatContent[] updated,in ThreatContent[] outdated)
        {
            List<ThreatContent> result = new List<ThreatContent>();
            int len = Math.Min(updated.Length, outdated.Length);
            for (int i = 0; i < len; ++i)
            {
                if (!Enumerable.SequenceEqual(updated[i].content ,outdated[i].content))
                {
                    result.Add(new ThreatContent(updated[i].content.Length));
                    for (int j = 0; j < updated[i].content.Length; ++j)
                    {
                        if (updated[i].content[j] == outdated[i].content[j])
                        {
                            result.Last().content[j] = updated[i].content[j];
                        }
                        else
                        {
                            result.Last().content[j] = "#БЫЛО:\n\n" + outdated[i].content[j] + "\n\n#Стало:\n\n" + updated[i].content[j];
                        }
                    }
                }
            }
            {   //different lengths
                int diff = updated.Length - outdated.Length;
                ThreatContent[] refTmp = null;
                if (diff != 0)
                {
                    string info = "";
                    if (diff > 0)
                    {
                        refTmp = updated;
                        info = "#Строка добавлена при обновлении\n\n";
                    }
                    else
                    {
                        refTmp = outdated;
                        info = "#Строка была удалена при обновлении\n\n";
                    }
                    for (int i = len; i < refTmp.Length; ++i)
                    {
                        result.Add(refTmp[i]);
                        result.Last().content[1] = info + result.Last().content[1];
                    }
                }
            }
            return result.ToArray();
        }
        public void OpenLocalTable(string path) //null or empty path stands for default path
        {
            if (string.IsNullOrEmpty(path))
            {
                path = Directory.GetCurrentDirectory() + "\\" + _defaultTableName;
            }
            CurrentTablePath = path;
            CurrentTableName = path.Split('\\').Last();
            OpenExcelTable();
        }

        public ShortInfo[] GetShortContent(ContentMode m)
        {
            ThreatContent[] data = TableData(m);
            if (data == null)
            {
                throw new InvalidOperationException();
            }
            ShortInfo[] result = new ShortInfo[data.Length];
            for (int i = 0; i < data.Length; ++i)
            {
                result[i] = new ShortInfo("УБИ." + data[i].content[0].PadLeft(3, '0'), data[i].content[1]);
            }
            return result;
        }
        private ThreatContent[] TableData(ContentMode m)
        {
            switch (m)
            {
                case ContentMode.Normal:
                    return _tableContentNormal;
                case ContentMode.Changed:
                    return _tableContentChanged;
                default:
                    return null;
            }
        }

        public FullInfo GetFullContent(ContentMode m, int threatId)
        {
            ThreatContent[] data = TableData(m);
            if (m == ContentMode.Changed) //in array of changed vals threadId does NOT correlate with array idxs (it contains only changed vals 
            {
                threatId = Array.FindIndex(data, (x) => int.Parse(x.content[0]) == threatId);
            }
            else //in array of normal vals threadId does correlate with array idxs but it is greater by 1 so ...
            {
                threatId -= 1;
            }
            if (threatId < 0) { throw new ArgumentOutOfRangeException(); }
            if (threatId > data.Length)
            {
                threatId = data.Length;
            }
            return new FullInfo(_headers.Zip(data[threatId].content, (x, y) => new Tuple<string, string>(x, y)).ToArray());
        }

        private static void LoadNewTableWeb(string uri, string pathToSave)
        {
            new WebClient().DownloadFile(uri, pathToSave);
        }
    }
}
