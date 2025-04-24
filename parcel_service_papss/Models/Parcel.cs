using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParcelService.Models
{
    public class Parcel
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int Depth { get; set; }
        /// <summary>
        /// This increases the delivery const by 30%.
        /// </summary>
        public bool IsFragile { get; set; }
        /// <summary>
        /// This doubles the delivery cost.
        /// </summary>
        public bool IsPriority { get; set; }
    }
}
