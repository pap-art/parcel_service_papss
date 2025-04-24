using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ParcelService.Enums;

namespace ParcelService.Models
{
    public class ParcelShelf
    {
        public int Id { get; set; }
        public ParcelShelfType Type { get; set; }
    }
}
