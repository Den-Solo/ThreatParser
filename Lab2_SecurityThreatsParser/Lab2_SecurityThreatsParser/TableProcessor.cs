﻿using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Windows;
using System.Data;
using ExcelDataReader;

namespace Lab2_SecurityThreatsParser
{
    public partial class TableProcessor
    {
        private const string _defaultUri = @"https://bdu.fstec.ru/files/documents/thrlist.xlsx";
        private const string _defaultTableName = "thrlist.xlsx";
        private readonly static string _defaultTablePath = Directory.GetCurrentDirectory() + "\\" + _defaultTableName;
        public string CurrentTablePath { get; private set; } = null;
        public string CurrentTableName { get; private set; } = null;
        public bool CanUpdate { get; private set; } = false;
        
        private string[] _headers;
        private ThreatContent[] _tableContentNormal;
        private ThreatContent[] _tableContentChanged;

        private DataSet GetTableContentSet_ = null;

        public enum LoadMode
        {
            DownloadNew,
            OpenExisting,   
            DownloadUpdate,
            OpenUpdate
        }
        public enum ContentMode
        {
            Normal,         //values displayed in main grid - all threats
            Changed         //threats which were changed by updated vals
        }
        public enum LoadStatus
        {
            OK,
            NetWorkProblems,
            FileProblems,
            SameFile
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
        private ThreatContent[] GetTableContent(ContentMode m)
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
        public LoadStatus LoadOrOpen(LoadMode lm, string localPath)
        {
            if (LoadModeToContentMode(lm) == ContentMode.Changed)       //if update then...
            {
                if (!CanUpdate)                                         //must be checked or handled by caller
                {
                    throw new InvalidOperationException("No update before first load");
                }
                if (localPath == CurrentTablePath)                      //impossible to update data base with the same data base
                {
                    return LoadStatus.FileProblems;
                }
                File.Delete(CurrentTablePath + ".deprecated");          //deleted .deprecated forever (if any)
                if (File.Exists(CurrentTablePath))                      //make current DB deprecated to give space for a new one
                {
                    File.Move(CurrentTablePath, CurrentTablePath + ".deprecated");
                }
            }
            if (!string.IsNullOrWhiteSpace(localPath) && (lm == LoadMode.OpenExisting || lm == LoadMode.OpenUpdate))
            {
                File.Copy(localPath, _defaultTablePath);                 //Database will always be in current working dir
                localPath = null;
            }
            CurrentTableName = _defaultTableName;                        //it must be different but i rejected my idea of allowing a user to choose database to open
            CurrentTablePath = _defaultTablePath;                        //so yeah CurrentTableName and  CurrentTablePath are always default
                                                                        
            bool isException = false;
            LoadStatus loadStatus = LoadStatus.OK;
            try
            {
                if (lm == LoadMode.DownloadNew || lm == LoadMode.DownloadUpdate)
                {
                    LoadNewTableWeb(TableProcessor._defaultUri, CurrentTablePath);
                }
                OpenExcelTable();
                ReadTableAll(LoadModeToContentMode(lm));
            }
            catch (WebException)
            {
                isException = true;
                loadStatus = LoadStatus.NetWorkProblems;
            }
            catch (Exception)
            {
                isException = true;
                loadStatus = LoadStatus.FileProblems;
            }
            finally
            {
                if (isException && File.Exists(CurrentTablePath + ".deprecated"))
                {
                    File.Move(CurrentTablePath + ".deprecated", CurrentTablePath); //shit, go back, go back (if download failed)
                }
            }
            if (LoadModeToContentMode(lm) == ContentMode.Normal && !isException)
            {
                CanUpdate = true;
            }
            return loadStatus;
        }
        public void OpenExcelTable() // CurrentTablePath must be set correctly before
        {
            using (var fs = File.OpenRead(CurrentTablePath)) 
            {
                using (var reader = ExcelReaderFactory.CreateReader(fs))
                {
                    GetTableContentSet_ = reader.AsDataSet();
                }
            }
        }
        private void ReadTableAll(ContentMode m)
        {
            if (m == ContentMode.Normal)
            {
                ReadHeader(GetTableContentSet_,ref _headers);
                ReadAllLines(GetTableContentSet_,ref  _tableContentNormal);
            }
            else
            {
                _tableContentChanged = _tableContentNormal;
                ReadAllLines(GetTableContentSet_,ref _tableContentNormal);
                _tableContentChanged = GetChangedThreats(_tableContentNormal, _tableContentChanged);
            }
            GetTableContentSet_ = null;
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
                    dest[i - rowIdxFirst].content[j] = (dest[i - rowIdxFirst].content[j] == "1" ? "Да" : "Нет");
                }
            }
        }
        private static ThreatContent[] GetChangedThreats(in ThreatContent[] updated,in ThreatContent[] outdated)
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
                            result.Last().content[j] = "###БЫЛО:\n\n" + outdated[i].content[j] + "\n\n###СТАЛО:\n\n" + updated[i].content[j];
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
                        result.Add(new ThreatContent((string[])refTmp[i].content.Clone()));
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

        public ShortInfo[] GetShortContent(ContentMode m) // called once to get all
        {
            ThreatContent[] data = GetTableContent(m);
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


        public FullInfo GetFullContent(ContentMode m, int threatId)
        {
            ThreatContent[] data = GetTableContent(m);
            if (m == ContentMode.Changed)       //in array of changed vals threadId does NOT correlate with array idxs (it contains only changed vals)
            {
                threatId = Array.FindIndex(data, (x) => int.Parse(x.content[0]) == threatId);
            }
            else                                //in array of normal vals threadId does correlate with array idxs but it is greater by 1 so ...
            {
                threatId -= 1;
            }
            if (threatId < 0) { return new FullInfo(); }
            if (threatId > data.Length)
            {
                threatId = data.Length - 1;
            }
            return new FullInfo(_headers.Zip(data[threatId].content, (x, y) => new Tuple<string, string>(x, y)).ToArray());
        }

        private static void LoadNewTableWeb(string uri, string pathToSave)
        {
            new WebClient().DownloadFile(uri, pathToSave);
        }
        public bool SaveAs(string path)
        {
            if (path == CurrentTablePath)
            {
                return false;
            }
            if (File.Exists(path))
            {
                File.Delete(path);
            }
           
            File.Copy(CurrentTablePath, path);
            return true;
         
        }
    }
}
