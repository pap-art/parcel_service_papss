using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParcelService.Models
{
    public class ParcelLocker
    {
        public int Id { get; set; }
        public int DistanceToBase { get; set; }
        public ParcelShelf[] ParcelShelves { get; set; } = Array.Empty<ParcelShelf>();
    }
}
