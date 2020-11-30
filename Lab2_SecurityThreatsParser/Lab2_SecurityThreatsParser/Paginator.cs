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
        private int firstIdx_ = 0;                  
        public int CurElementsOnPageCnt_ { get; private set; } = 0;
        private DataGrid dataGrid_ = null;          //dataGrid to work with
        private object[] data_ = null;              //object array to paginate
        public bool CanGoPrev 
        { 
            get 
            {
                return firstIdx_ != 0;
            }
        }
        public bool CanGoNext
        {
            get
            {
                return firstIdx_ + CurElementsOnPageCnt_ < data_.Length;
            }
        }

        public Paginator(DataGrid dataGrid, object[] data, int elementsOnPage)
        {
            dataGrid_ = dataGrid;
            dataGrid_.ItemsSource = new List<object>() { new TableProcessor.ShortInfo("","Нет записей")};     
            this.data_ = data;
            this.CurElementsOnPageCnt_ = elementsOnPage;
            this.firstIdx_ -= elementsOnPage;           // to start from beginning with Next() firstIdx must be neg(elementsOnPage)
        }
        public bool Next()
        {
            firstIdx_ += CurElementsOnPageCnt_;
            if (firstIdx_ > data_.Length)
            {
                firstIdx_ = data_.Length;
            }
            int len = (firstIdx_ + CurElementsOnPageCnt_ <= data_.Length ? CurElementsOnPageCnt_ : data_.Length - firstIdx_);
            if (len > 0)
            {
                DataGridSetRows(dataGrid_, Extentions.SubArray(data_, firstIdx_, len).ToList());
            }
            return CanGoNext;
        }
        public bool Prev()
        {
            firstIdx_ -= CurElementsOnPageCnt_;
            if (firstIdx_ < 0)
            {
                firstIdx_ = 0;
            }
            int len = (firstIdx_ + CurElementsOnPageCnt_ <= data_.Length ? CurElementsOnPageCnt_ : data_.Length - firstIdx_);
            DataGridSetRows(dataGrid_, Extentions.SubArray(data_, firstIdx_, len).ToList());
            return CanGoPrev;
        }

        public void Update(int newElementsOnPageCnt)
        {
            if (newElementsOnPageCnt > data_.Length)
            {
                newElementsOnPageCnt = data_.Length;
            }
            CurElementsOnPageCnt_ = newElementsOnPageCnt;
            if (firstIdx_ + CurElementsOnPageCnt_ > data_.Length)
            {
                firstIdx_ = data_.Length - CurElementsOnPageCnt_;
            }
            firstIdx_ -= CurElementsOnPageCnt_;
            Next();
        }
        private static void DataGridSetRows(DataGrid dg, in List<object> newRows)
        {
            var container = (List<object>)dg.ItemsSource;
            dg.ItemsSource = null;
            container = newRows;
            dg.ItemsSource = container;
        }
    }
}
