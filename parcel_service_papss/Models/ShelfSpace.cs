using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ParcelService.Enums;

namespace ParcelService.Models
{
    public class ShelfSpace
    {
        

        public int AvailableWidth { get; set; }
        public int AvailableHeight { get; set; }
        public int AvailableDepth { get; set; }
        public List<int> ParcelIds { get; set; } = new List<int>();

        // Shelf dimensions
        public const int SMALL_SHELF_WIDTH = 50;
        public const int SMALL_SHELF_HEIGHT = 60;
        public const int SMALL_SHELF_DEPTH = 70;

        public const int MEDIUM_SHELF_WIDTH = 100;
        public const int MEDIUM_SHELF_HEIGHT = 60;
        public const int MEDIUM_SHELF_DEPTH = 200;

        public const int LARGE_SHELF_WIDTH = 400;
        public const int LARGE_SHELF_HEIGHT = 100;
        public const int LARGE_SHELF_DEPTH = 300;

        public static ShelfSpace CreateForShelfType(ParcelShelfType shelfType)
        {
            return shelfType switch
            {
                ParcelShelfType.Small => new ShelfSpace
                {
                    AvailableWidth = SMALL_SHELF_WIDTH,
                    AvailableHeight = SMALL_SHELF_HEIGHT,
                    AvailableDepth = SMALL_SHELF_DEPTH
                },
                ParcelShelfType.Medium => new ShelfSpace
                {
                    AvailableWidth = MEDIUM_SHELF_WIDTH,
                    AvailableHeight = MEDIUM_SHELF_HEIGHT,
                    AvailableDepth = MEDIUM_SHELF_DEPTH
                },
                ParcelShelfType.Large => new ShelfSpace
                {
                    AvailableWidth = LARGE_SHELF_WIDTH,
                    AvailableHeight = LARGE_SHELF_HEIGHT,
                    AvailableDepth = LARGE_SHELF_DEPTH
                },
                _ => throw new ArgumentOutOfRangeException(nameof(shelfType))
            };
        }
    }
}
