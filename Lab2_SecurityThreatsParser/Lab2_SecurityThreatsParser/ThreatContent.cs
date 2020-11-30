namespace Lab2_SecurityThreatsParser
{
    public partial class TableProcessor
    {
        private class ThreatContent
        {
            public string[] content;
            public ThreatContent(int sz)
            {
                content = new string[sz];
            }
            public ThreatContent(string[] data)
            {
                content = data;
            }
        }
    }
}
