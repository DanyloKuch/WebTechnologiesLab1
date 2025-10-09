namespace WebTechnologiesLab1.Models
{
    public class Product
    {
        public int id { get; set; }

        public string? name { get; set; }

        public decimal price { get; set; }

        public string? description { get; set; }

        public string? imageUrl { get; set; }

        public int CategoryId { get; set; }
        public Category? Category { get; set; }
    }
}
