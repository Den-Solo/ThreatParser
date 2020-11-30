using System;
using System.Collections.Generic;

namespace Lab2_SecurityThreatsParser
{
    public partial class TableProcessor
    {
        public class FullInfo : List<Tuple<string,string>> //data class
        { 
            public FullInfo() { }
            public FullInfo(Tuple<string,string>[] data) : base(data)
            {
            }
        }
    }
}
