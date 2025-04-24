using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ParcelService.Enums;
using ParcelService.Exceptions;
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
        private readonly Dictionary<int, ShelfSpace> _shelfSpaces = new Dictionary<int, ShelfSpace>();
        private int NEXT_DELIVERY_ID = 1;
        private readonly Random RANDOM = new Random();


        public (int DeliveryId, string SecurityCode, decimal Price) ClientSend(Parcel parcel, int fromLockerId, int toLockerId)
        {

            var (fromLocker, toLocker) = GetSourceAndDestinationLockers(fromLockerId, toLockerId);

            var (shelf, price) = FindShelfAndCalculatePrice(fromLocker, toLocker, parcel);
            var delivery = CreateDeliveryRecord(parcel, fromLockerId, toLockerId, price, shelf.Id);
            AddParcelToShelf(shelf, delivery);
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

            return ApplyPriceModifiers(basePrice, parcel);
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
                ParcelShelfType.Small => SMALL_SHELF_PRICE_PER_UNIT,
                ParcelShelfType.Medium => MEDIUM_SHELF_PRICE_PER_UNIT,
                ParcelShelfType.Large => LARGE_SHELF_PRICE_PER_UNIT,
                _ => throw new ArgumentOutOfRangeException(nameof(shelfType))
            };
        }

        private decimal ApplyPriceModifiers(decimal basePrice, Parcel parcel)
        {
            if (parcel.IsFragile)
            {
                basePrice *= FRAGILE_ITEM_MULTIPLIER;
            }

            if (parcel.IsPriority)
            {
                basePrice *= PRIORITY_ITEM_MULTIPLIER; 
            }

            return decimal.Round(basePrice, 2, MidpointRounding.AwayFromZero);
        }

        private ParcelShelf FindSuitableShelf(ParcelLocker locker, Parcel parcel)
        {
            // Initialize any shelves that haven't been used before
            foreach (var shelf in locker.ParcelShelves)
            {
                InitializeShelfSpaceIfNeeded(shelf);
            }

            // Find shelves with enough available space
            var suitableShelves = locker.ParcelShelves
                .Where(shelf => DoesFitOnShelf(parcel, shelf))
                .ToList();

            if (!suitableShelves.Any())
            {
                return null;
            }

            // Find the shelf with the least available space to optimize usage
            return suitableShelves
                .OrderBy(shelf => _shelfSpaces[shelf.Id].AvailableWidth *
                                 _shelfSpaces[shelf.Id].AvailableHeight *
                                 _shelfSpaces[shelf.Id].AvailableDepth)
                .First();
        }

        private void InitializeShelfSpaceIfNeeded(ParcelShelf shelf)
        {
            if (!_shelfSpaces.ContainsKey(shelf.Id))
            {
                _shelfSpaces[shelf.Id] = ShelfSpace.CreateForShelfType(shelf.Type);
            }
        }

        private void AddParcelToShelf(ParcelShelf shelf, DeliveryRecord delivery)
        {
            InitializeShelfSpaceIfNeeded(shelf);

            var shelfSpace = _shelfSpaces[shelf.Id];
            shelfSpace.ParcelIds.Add(delivery.Id);
            UpdateAvailableSpace(shelfSpace, delivery.Parcel, false);
        }

        private void RemoveParcelFromShelf(int shelfId, int parcelId, Parcel parcel)
        {
            if (_shelfSpaces.TryGetValue(shelfId, out var shelfSpace))
            {
                shelfSpace.ParcelIds.Remove(parcelId);
                UpdateAvailableSpace(shelfSpace, parcel, true);
            }
        }

        private void UpdateAvailableSpace(ShelfSpace shelfSpace, Parcel parcel, bool isRemoval)
        {
            if (isRemoval)
            {
                // When removing, increase available space (simplified model)
                // In a real system, this would handle complex space management
                shelfSpace.AvailableWidth += parcel.Width;
                shelfSpace.AvailableHeight += parcel.Height;
                shelfSpace.AvailableDepth += parcel.Depth;
            }
            else
            {
                // When adding, decrease available space
                shelfSpace.AvailableWidth -= parcel.Width;
                shelfSpace.AvailableHeight -= parcel.Height;
                shelfSpace.AvailableDepth -= parcel.Depth;
            }
        }

        private bool CanFitOnShelfSpace(Parcel parcel, ShelfSpace shelfSpace)
        {
            return parcel.Width <= shelfSpace.AvailableWidth &&
                  parcel.Height <= shelfSpace.AvailableHeight &&
                  parcel.Depth <= shelfSpace.AvailableDepth;
        }

        private bool DoesFitOnShelf(Parcel parcel, ParcelShelf shelf)
        {

            if (!DoesFitOnShelfType(parcel, shelf.Type))
            {
                return false;
            }

            // Then check if there's available space on this specific shelf
            InitializeShelfSpaceIfNeeded(shelf);
            return CanFitOnShelfSpace(parcel, _shelfSpaces[shelf.Id]);
        }

        private bool DoesFitOnShelfType(Parcel parcel, ParcelShelfType shelfType)
        {
            var space = ShelfSpace.CreateForShelfType(shelfType);

            return parcel.Width <= space.AvailableWidth &&
                  parcel.Height <= space.AvailableHeight &&
                  parcel.Depth <= space.AvailableDepth;
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
        public int[] PickUpParcelsFromLocker(int lockerId)
        {
            GetLockerById(lockerId); // Validate locker exists

            var deliveriesToPickUp = GetAwaitingPickupDeliveries(lockerId);
            if (!deliveriesToPickUp.Any())
            {
                return Array.Empty<int>();
            }

            var prioritizedDeliveries = PrioritizeDeliveriesForPickup(deliveriesToPickUp);
            return ProcessPickupDeliveries(prioritizedDeliveries);
        }

        private List<DeliveryRecord> GetAwaitingPickupDeliveries(int lockerId)
        {
            return _deliveries
                .Where(d => d.FromLockerId == lockerId && d.Status == DeliveryStatus.AwaitingPickup)
                .ToList();
        }

        private List<DeliveryRecord> PrioritizeDeliveriesForPickup(List<DeliveryRecord> deliveries)
        {
            return deliveries
                .OrderByDescending(d => d.Parcel.IsPriority)
                .ThenBy(d => d.CreatedAt)
                .ToList();
        }

        private int[] ProcessPickupDeliveries(List<DeliveryRecord> prioritizedDeliveries)
        {
            var pickedParcelIds = new List<int>();
            var (currentWidth, currentHeight, currentDepth) = (0, 0, 0);

            foreach (var delivery in prioritizedDeliveries)
            {
                if (CanFitInCourierVehicle(delivery.Parcel, ref currentWidth, ref currentHeight, ref currentDepth,
                    COURIER_VEHICLE_MAX_WIDTH, COURIER_VEHICLE_MAX_HEIGHT, COURIER_VEHICLE_MAX_DEPTH) || delivery.Parcel.IsPriority)
                {
                    // Free the shelf space when parcel is picked up
                    if (delivery.LocationShelfId.HasValue)
                    {
                        RemoveParcelFromShelf(delivery.LocationShelfId.Value, delivery.Id, delivery.Parcel);
                    }

                    UpdateDeliveryForPickup(delivery);
                    pickedParcelIds.Add(delivery.Id);
                }
            }

            return pickedParcelIds.ToArray();
        }

        private bool CanFitInCourierVehicle(
            Parcel parcel, ref int currentWidth, ref int currentHeight, ref int currentDepth,
            int maxWidth, int maxHeight, int maxDepth)
        {
            if (currentWidth + parcel.Width <= maxWidth &&
                currentHeight + parcel.Height <= maxHeight &&
                currentDepth + parcel.Depth <= maxDepth)
            {
                // Update dimensions used
                currentWidth += parcel.Width;
                currentHeight = Math.Max(currentHeight, parcel.Height);
                currentDepth = Math.Max(currentDepth, parcel.Depth);

                return true;
            }

            return false;
        }

        private void UpdateDeliveryForPickup(DeliveryRecord delivery)
        {
            delivery.Status = DeliveryStatus.InTransitToBase;
            delivery.PickedUpAt = DateTimeOffset.Now;
            delivery.ArrivedAtBaseAt = DateTimeOffset.Now;
            delivery.Status = DeliveryStatus.AtBase;
            delivery.LocationShelfId = null;
        }

        public int[] DeliverParcels(int lockerId)
        {
            var locker = GetLockerById(lockerId);

            var deliveriesToDeliver = _deliveries
                .Where(d => d.ToLockerId == lockerId && d.Status == DeliveryStatus.AtBase)
                .ToList();

            return PlaceDeliveriesInLocker(deliveriesToDeliver, locker);
        }

        private int[] PlaceDeliveriesInLocker(List<DeliveryRecord> deliveries, ParcelLocker locker)
        {
            if (!deliveries.Any())
            {
                return Array.Empty<int>();
            }

            return deliveries
                .Where(d => ProcessDeliveryToLocker(d, locker))
                .Select(d => d.Id)
                .ToArray();
        }

        private bool ProcessDeliveryToLocker(DeliveryRecord delivery, ParcelLocker locker)
        {
            var shelf = FindSuitableShelf(locker, delivery.Parcel);

            if (shelf != null)
            {
                delivery.Status = DeliveryStatus.AwaitingCollection;
                delivery.DeliveredToLockerAt = DateTimeOffset.Now;
                delivery.LocationShelfId = shelf.Id;
                AddParcelToShelf(shelf, delivery);

                return true;
            }

            return false;
        }


        public int ClientReceive(int deliveryId, string securityCode)
        {
            var delivery = ValidateClientReceiveInputs(deliveryId, securityCode);
            var shelfId = delivery.LocationShelfId ??
                throw new InvalidOperationException("Shelf ID not assigned");

            // Update delivery status and free the space on the shelf
            delivery.Status = DeliveryStatus.Delivered;
            delivery.CollectedAt = DateTimeOffset.Now;
            RemoveParcelFromShelf(shelfId, delivery.Id, delivery.Parcel);

            return shelfId;
        }


        private DeliveryRecord ValidateClientReceiveInputs(int deliveryId, string securityCode)
        {
            var delivery = _deliveries.FirstOrDefault(d => d.Id == deliveryId);

            if (delivery == null)
            {
                throw new ArgumentNullException($"Delivery with ID {deliveryId} not found");
            }

            if (delivery.SecurityCode != securityCode)
            {
                throw new InvalidSecurityCodeException("Invalid security code provided");
            }

            if (delivery.Status != DeliveryStatus.AwaitingCollection)
            {
                throw new InvalidOperationException($"Delivery is not ready for collection. Current status: {delivery.Status}");
            }

            return delivery;
        }
    }
}
