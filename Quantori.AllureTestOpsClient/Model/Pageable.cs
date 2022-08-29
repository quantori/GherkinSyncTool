namespace Quantori.AllureTestOpsClient.Model
{
    public class Pageable
    {
        public int Offset { get; set; }
        public Sort Sort { get; set; }
        public int PageSize { get; set; }
        public bool Unpaged { get; set; }
        public int PageNumber { get; set; }
        public bool Paged { get; set; }
    }
}