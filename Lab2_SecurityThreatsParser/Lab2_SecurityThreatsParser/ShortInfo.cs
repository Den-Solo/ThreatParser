namespace Lab2_SecurityThreatsParser
{
    public partial class TableProcessor
    {

        public class ShortInfo //data class
        {
            public string Id { get; set; }
            public string Name { get; set; }
           
            public ShortInfo(string id, string name)
            {
                Id = id;
                Name = name;
            }
        }
    }
}
