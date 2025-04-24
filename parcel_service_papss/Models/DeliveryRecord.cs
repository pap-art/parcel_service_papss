using ParcelService.Enums;

namespace ParcelService.Models
{
    public class DeliveryRecord
    {
        public int Id { get; set; }
        public Parcel Parcel { get; set; }
        public int FromLockerId { get; set; }
        public int ToLockerId { get; set; }
        public string SecurityCode { get; set; }
        public decimal Price { get; set; }
        public DeliveryStatus Status { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? PickedUpAt { get; set; }
        public DateTimeOffset? ArrivedAtBaseAt { get; set; }
        public DateTimeOffset? DeliveredToLockerAt { get; set; }
        public DateTimeOffset? CollectedAt { get; set; }
        public int? LocationShelfId { get; set; }
    }
}