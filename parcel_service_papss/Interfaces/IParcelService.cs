using ParcelService.Models;

namespace ParcelService.Interfaces;
/// <summary>
/// Implements simplified parcel delivery service functionality.
/// The service operates in a centralized fashion:
/// first, all parcels are delivered to a base, and then they are sent to their destination.
/// </summary>
public interface IParcelService
{
    string CompanyName { get; set; }
    /// <summary>
    /// List of available lockers.
    /// </summary>
    public ParcelLocker[] ParcelLockers { get; set; }
    /// <summary>
    /// This method is called by the client to send a parcel to another client.
    /// </summary>
    /// <param name="parcel">Parcel to send.</param>
    /// <param name="fromLockerId">Starting location.</param>
    /// <param name="toLockerId">Destination.</param>
    /// <returns>
    /// A security code is required to receive the parcel.
    /// The cost is calculated based on the distance between locations and the base, fragility, priority, and parcel size.
    /// </returns>
    (int DeliveryId, string SecurityCode, decimal Price) ClientSend(Parcel parcel, int fromLockerId, int toLockerId);
    /// <summary>
    /// This method is used by the client to receive a parcel.
    /// </summary>
    /// <param name="deliveryId"></param>
    /// <param name="securityCode"></param>
    /// <returns>Shelf ID from which to pick up the parcel.</returns>
    int ClientReceive(int deliveryId, string securityCode);
    /// <summary>
    /// This method is used by delivery personnel to pick up parcels from lockers.
    /// Note: the delivery transport has a space limit of 800x200x200 units.
    /// Priority items should be picked first if all parcels do not fit.
    /// Assume all parcels are delivered to the base as soon as this action is called.
    /// </summary>
    /// <param name="lockerId">Locker from which parcels must be taken.</param>
    /// <returns>List of delivery Ids.</returns>
    int[] PickUpParcelsFromLocker(int lockerId);
    /// <summary>
    /// This method is used by delivery personnel to get the list of parcels that need to be delivered.
    /// Assume all parcels are delivered to the locker as soon as this action is called.
    /// </summary>
    /// <returns>List of delivery Ids.</returns>
    int[] DeliverParcels(int lockerId);
    /// <summary>
    /// Gets total income for each month.
    /// </summary>
    /// <returns></returns>
    (int Year, int Month, decimal Income) GetMonthlyIncomeReport();
    /// <summary>
    /// Gets the average parcel delivery time for a given period.
    /// </summary>
    /// <param name="periodStart"></param>
    /// <param name="periodEnd"></param>
    /// <returns></returns>
    TimeSpan GetAverageDeliveryTime(DateTimeOffset periodStart, DateTimeOffset periodEnd);
}