using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParcelService.Enums
{
    public enum ParcelShelfType
    {
        /// <summary>
        /// Dimensions: 50x60x70 units
        /// 0.05 EUR per distance unit
        /// </summary>
        Small,
        /// <summary>
        /// Dimensions: 100x60x200 units
        /// 0.10 EUR per distance unit
        /// </summary>
        Medium,
        /// <summary>
        /// Dimensions: 400x100x300 units
        /// 0.40 EUR per distance unit
        /// </summary>
        Large
    }
}
