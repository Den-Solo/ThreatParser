using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using Lab2_SecurityThreatsParser;

namespace Lab2_GUI
{
    public class Paginator
    {
        private int _firstIdx = 0;                  
        public int _ElementsOnPageCnt { get; private set; } = 0;    //val which was set by Update()
        public int _realElementsOnPageCnt = 0;                      //real val based on data.Length
        private DataGrid _dataGrid = null;                          //dataGrid to work with
        private object[] _data = null;                              //object array to paginate
        public bool _CanGoPrev  {  get { return _firstIdx != 0; } }
        public bool _CanGoNext { get { return _firstIdx + _realElementsOnPageCnt < _data.Length; } }
        public int _DataLength { get; set; }
        public Paginator(DataGrid dataGrid, object[] data, int elementsOnPage)
        {
            _DataLength = data.Length;
            this._dataGrid = dataGrid;
            this._data = ((data == null || data.Length == 0) ? new object[] { new TableProcessor.ShortInfo("XXX.000", "Нет записей") } : data);
            this._ElementsOnPageCnt = elementsOnPage;
            this._realElementsOnPageCnt = Math.Min(elementsOnPage, this._data.Length);
            this._firstIdx -= this._realElementsOnPageCnt;           // to start from beginning with Next() firstIdx must be neg(elementsOnPage)
        }
        public bool Next()
        {
            _firstIdx += _realElementsOnPageCnt;
            if (_firstIdx > _data.Length)
            {
                _firstIdx = _data.Length;
            }
            int len = (_firstIdx + _realElementsOnPageCnt <= _data.Length ? _realElementsOnPageCnt : _data.Length - _firstIdx);
            if (len > 0)
            {
                DataGridSetRows(_dataGrid, Extentions.SubArray(_data, _firstIdx, len).ToList());
            }
            return _CanGoNext;
        }
        public bool Prev()
        {
            _firstIdx -= _realElementsOnPageCnt;
            if (_firstIdx < 0)
            {
                _firstIdx = 0;
            }
            int len = (_firstIdx + _realElementsOnPageCnt <= _data.Length ? _realElementsOnPageCnt : _data.Length - _firstIdx);
            DataGridSetRows(_dataGrid, Extentions.SubArray(_data, _firstIdx, len).ToList());
            return _CanGoPrev;
        }

        public void Update(int newElementsOnPageCnt)
        {
            _ElementsOnPageCnt = newElementsOnPageCnt;
            if (newElementsOnPageCnt > _data.Length)
            {
                newElementsOnPageCnt = _data.Length;
            }
            _realElementsOnPageCnt = newElementsOnPageCnt;
            if (_firstIdx + _realElementsOnPageCnt > _data.Length)
            {
                _firstIdx = _data.Length - _realElementsOnPageCnt;
            }
            _firstIdx -= _realElementsOnPageCnt;
            Next();
        }
        private static void DataGridSetRows(DataGrid dg, in List<object> newRows)
        {
            dg.ItemsSource = null;
            dg.ItemsSource = newRows;
        }
    }
}
