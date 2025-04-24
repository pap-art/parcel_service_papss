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
         
        public ParcelLocker[] ParcelLockers { get; set; }


        // Security code
        private const int SECURITY_CODE_LENGTH = 6;
        private const int NUMBER_OF_DIGITS = 10; // 0-9

        // Shelf dimensions
        private const int SMALL_SHELF_WIDTH = 50;
        private const int SMALL_SHELF_HEIGHT = 60;
        private const int SMALL_SHELF_DEPTH = 70;

        private const int MEDIUM_SHELF_WIDTH = 100;
        private const int MEDIUM_SHELF_HEIGHT = 60;
        private const int MEDIUM_SHELF_DEPTH = 200;

        private const int LARGE_SHELF_WIDTH = 400;
        private const int LARGE_SHELF_HEIGHT = 100;
        private const int LARGE_SHELF_DEPTH = 300;

        // Courier vehicle dimensions  
        private const int COURIER_VEHICLE_MAX_WIDTH = 800;
        private const int COURIER_VEHICLE_MAX_HEIGHT = 200;
        private const int COURIER_VEHICLE_MAX_DEPTH = 200;

        // Price constants  
        private const decimal SMALL_SHELF_PRICE_PER_UNIT = 0.05m;
        private const decimal MEDIUM_SHELF_PRICE_PER_UNIT = 0.10m;
        private const decimal LARGE_SHELF_PRICE_PER_UNIT = 0.40m;

        private const decimal FRAGILE_ITEM_MULTIPLIER = 1.3m;
        private const decimal PRIORITY_ITEM_MULTIPLIER = 2.0m;

        private List<DeliveryRecord> _deliveries = new List<DeliveryRecord>();
        private int NEXT_DELIVERY_ID = 1;
        private readonly Random RANDOM = new Random();

        public (int DeliveryId, string SecurityCode, decimal Price) ClientSend(Parcel parcel, int fromLockerId, int toLockerId)
        {

            var (fromLocker, toLocker) = GetSourceAndDestinationLockers(fromLockerId, toLockerId);

            var (shelf, price) = FindShelfAndCalculatePrice(fromLocker, toLocker, parcel);
            var delivery = CreateDeliveryRecord(parcel, fromLockerId, toLockerId, price, 0);
            _deliveries.Add(delivery); // Fixed the issue by using Add method directly without assignment  

            return (delivery.Id, delivery.SecurityCode, delivery.Price);
        }

        private DeliveryRecord CreateDeliveryRecord(Parcel parcel, int fromLockerId, int toLockerId, decimal price, int shelfId)
        {
            return new DeliveryRecord
            {
                Id = NEXT_DELIVERY_ID++,
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

        public string GenerateSecurityCode()
        {
            return new string(
                      Enumerable.Range(0, SECURITY_CODE_LENGTH) 
                     .Select(_ => RANDOM.Next(0, NUMBER_OF_DIGITS))  
                     .Select(n => n.ToString()[0])   
                     .ToArray());
        }

        private (ParcelShelf Shelf, decimal Price) FindShelfAndCalculatePrice(
            ParcelLocker fromLocker, ParcelLocker toLocker, Parcel parcel)
        {


            var shelf = FindSuitableShelf(fromLocker, parcel);

            if (shelf == null)
            {
                throw new ArgumentNullException(nameof(shelf), $"No suitable shelf found in locker {fromLocker.Id} for the given parcel dimensions");
                //throw new NoSuitableShelfException(
                //    $"No suitable shelf found in locker {fromLocker.Id} for the given parcel dimensions");
            }

            var price = CalculatePrice(parcel, fromLocker, toLocker, shelf.Type);

            return (shelf, price);
        }

        private decimal CalculatePrice(
            Parcel parcel, ParcelLocker fromLocker, ParcelLocker toLocker, ParcelShelfType shelfType)
        {
            var basePrice = CalculateBasePrice(fromLocker, toLocker, shelfType);
            //return ApplyPriceModifiers(basePrice, parcel);
            return basePrice;
        }

        private decimal CalculateBasePrice(
            ParcelLocker fromLocker, ParcelLocker toLocker, ParcelShelfType shelfType)
        {
            var pricePerUnit = GetPricePerDistanceUnit(shelfType);

            var totalDistance = fromLocker.DistanceToBase + toLocker.DistanceToBase;

            return totalDistance * pricePerUnit;
        }

        private decimal GetPricePerDistanceUnit(ParcelShelfType shelfType)
        {
            return shelfType switch
            {
                ParcelShelfType.Small => 0.05m,
                ParcelShelfType.Medium => 0.10m,
                ParcelShelfType.Large => 0.40m,
                _ => throw new ArgumentOutOfRangeException(nameof(shelfType))
            };
        }

        private ParcelShelf FindSuitableShelf(ParcelLocker locker, Parcel parcel)
        {
            return locker.ParcelShelves
                .FirstOrDefault(shelf => DoesFitOnShelf(parcel, shelf.Type));
        }

        private bool DoesFitOnShelf(Parcel parcel, ParcelShelfType shelfType)
        {

            return shelfType switch
            {
                ParcelShelfType.Small =>
                parcel.Width <= SMALL_SHELF_WIDTH && parcel.Height <= SMALL_SHELF_HEIGHT && parcel.Depth <= SMALL_SHELF_DEPTH,
                ParcelShelfType.Medium =>
                parcel.Width <= MEDIUM_SHELF_WIDTH && parcel.Height <= MEDIUM_SHELF_HEIGHT && parcel.Depth <= MEDIUM_SHELF_DEPTH,
                ParcelShelfType.Large =>
                parcel.Width <= LARGE_SHELF_WIDTH && parcel.Height <= LARGE_SHELF_HEIGHT && parcel.Depth <= LARGE_SHELF_DEPTH,
                _ => false
            };
        }


        private ParcelLocker GetLockerById(int lockerId)
        {
            var locker = ParcelLockers?.FirstOrDefault(l => l.Id == lockerId);

            if (locker == null)
            {
                throw new ArgumentNullException(nameof(lockerId), $"Locker with ID {lockerId} not found");
                //throw new LockerNotFoundException($"Locker with ID {lockerId} not found");
            }

            return locker;
        }

        private (ParcelLocker FromLocker, ParcelLocker ToLocker) GetSourceAndDestinationLockers(int fromLockerId, int toLockerId)
        {
            return (GetLockerById(fromLockerId), GetLockerById(toLockerId));
        }
    }
}
