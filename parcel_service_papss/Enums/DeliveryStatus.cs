using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParcelService.Enums
{
    public enum DeliveryStatus
    {
        AwaitingPickup,
        InTransitToBase,
        AtBase,
        InTransitToDestination,
        AwaitingCollection,
        Delivered
    }
}
