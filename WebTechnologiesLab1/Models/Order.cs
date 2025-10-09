namespace WebTechnologiesLab1.Models
{
    public class Order
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; }
        public decimal TotalAmount { get; set; }
        public string UserId { get; set; }
        public ApplicationUser? User { get; set; }
        public string DeliveryAddress { get; set; }
        public string PhoneNumber { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; }
    }
}
