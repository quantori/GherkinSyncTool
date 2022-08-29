using System.Collections.Generic;

namespace Quantori.AllureTestOpsClient.Model
{
    public class GetTestCasesResponse
    {
        public int TotalElements { get; set; }
        public int TotalPages { get; set; }
        public int Size { get; set; }
        public List<Content> Content { get; set; }
        public int Number { get; set; }
        public Sort Sort { get; set; }
        public int NumberOfElements { get; set; }
        public Pageable Pageable { get; set; }
        public bool First { get; set; }
        public bool Last { get; set; }
        public bool Empty { get; set; }
    }
}