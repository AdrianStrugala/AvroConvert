namespace SolTechnology.PerformanceBenchmark
{
    [Equals(DoNotAddEqualityOperators = true)]
    public class Dataset
    {
        public int min_position { get; set; }
        public bool has_more_items { get; set; }
        public string items_html { get; set; }
        public int new_latent_count { get; set; }
        public bool boolArray { get; set; }
    }
}