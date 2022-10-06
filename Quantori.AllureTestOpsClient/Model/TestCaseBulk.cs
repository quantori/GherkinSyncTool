using System.Collections.Generic;

namespace Quantori.AllureTestOpsClient.Model
{
    public class TestCaseBulk
    {
        public Selection Selection { get; set; }
    }
    
    public class Selection
    {
        public bool Inverted { get; set; }
        public List<List<long>> GroupsInclude { get; set; }
        public List<List<long>> GroupsExclude { get; set; }
        public List<long> LeafsInclude { get; set; }
        public List<long> LeafsExclude { get; set; }
        public List<long> Path { get; set; }
        public long ProjectId { get; set; }
        public long TreeId { get; set; }
        public long FilterId { get; set; }
        public string Search { get; set; }
    }
}