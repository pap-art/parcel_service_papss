using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ParcelService.Enums;
using ParcelService.Models;

namespace ParcelService
{
    public class ParcelServiceImpl
    {
        public string CompanyName { get; set; }
        /// <summary>  
        /// List of available lockers.  
        /// </summary>  
        public ParcelLocker[] ParcelLockers { get; set; }

        private List<DeliveryRecord> _deliveries = new List<DeliveryRecord>();
        private int _nextDeliveryId = 1;

        public (int DeliveryId, string SecurityCode, decimal Price) ClientSend(Parcel parcel, int fromLockerId, int toLockerId)
        {
            var delivery = CreateDeliveryRecord(parcel, fromLockerId, toLockerId, 0, 0);
            _deliveries.Add(delivery); // Fixed the issue by using Add method directly without assignment  

            return (delivery.Id, delivery.SecurityCode, delivery.Price);
        }

        private DeliveryRecord CreateDeliveryRecord(Parcel parcel, int fromLockerId, int toLockerId, decimal price, int shelfId)
        {
            return new DeliveryRecord
            {
                Id = _nextDeliveryId++,
                Parcel = parcel,
                FromLockerId = fromLockerId,
                ToLockerId = toLockerId,
                SecurityCode = GenerateSecurityCode(),
                Price = price,
                Status = DeliveryStatus.AwaitingPickup,
                CreatedAt = DateTimeOffset.Now,
                LocationShelfId = shelfId
            };
        }

        private string GenerateSecurityCode()
        {
            return "";
        }
    }
}
