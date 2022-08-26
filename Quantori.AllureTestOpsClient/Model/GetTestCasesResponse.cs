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
    
    public class Content
    {
        public ulong Id { get; set; }
        public string Name { get; set; }
        public Status Status { get; set; }
        public TestLayer TestLayer { get; set; }
        public bool Automated { get; set; }
    }
    
    public class Pageable
    {
        public int Offset { get; set; }
        public Sort Sort { get; set; }
        public int PageSize { get; set; }
        public bool Unpaged { get; set; }
        public int PageNumber { get; set; }
        public bool Paged { get; set; }
    }

    public class Sort
    {
        public bool Empty { get; set; }
        public bool Sorted { get; set; }
        public bool Unsorted { get; set; }
    }
}