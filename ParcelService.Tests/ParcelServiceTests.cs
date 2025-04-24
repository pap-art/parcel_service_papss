using System;
using System.Linq;
using ParcelService.Enums;
using ParcelService.Models;
using Xunit;

namespace ParcelService.Tests
{
    public class ParcelServiceTests
    {
        

        private ParcelServiceImpl CreateService()
        {
            // Create a service with some test lockers
            var service = new ParcelServiceImpl
            {
                CompanyName = "Test Parcel Service",
                ParcelLockers = new[]
                {
                    CreateTestLocker(1, 100, new[]
                    {
                        (1, ParcelShelfType.Small),
                        (2, ParcelShelfType.Medium),
                        (3, ParcelShelfType.Large)
                    }),
                    CreateTestLocker(2, 200, new[]
                    {
                        (4, ParcelShelfType.Small),
                        (5, ParcelShelfType.Medium),
                        (6, ParcelShelfType.Large)
                    }),
                    CreateTestLocker(3, 300, new[]
                    {
                        (7, ParcelShelfType.Small),
                        (8, ParcelShelfType.Medium),
                        (9, ParcelShelfType.Large)
                    })
                }
            };

            return service;
        }

        private ParcelLocker CreateTestLocker(int id, int distanceToBase, (int Id, ParcelShelfType Type)[] shelves)
        {
            return new ParcelLocker
            {
                Id = id,
                DistanceToBase = distanceToBase,
                ParcelShelves = shelves.Select(s => new ParcelShelf
                {
                    Id = s.Id,
                    Type = s.Type
                }).ToArray()
            };
        }

        private Parcel CreateSmallParcel(bool isFragile = false, bool isPriority = false)
        {
            return new Parcel
            {
                Width = 4,
                Height = 5,
                Depth = 6,
                IsFragile = isFragile,
                IsPriority = isPriority
            };
        }

        private Parcel CreateMediumParcel(bool isFragile = false, bool isPriority = false)
        {
            return new Parcel
            {
                Width = 90,
                Height = 50,
                Depth = 150,
                IsFragile = isFragile,
                IsPriority = isPriority
            };
        }

        private Parcel CreateLargeParcel(bool isFragile = false, bool isPriority = false)
        {
            return new Parcel
            {
                Width = 300,
                Height = 90,
                Depth = 250,
                IsFragile = isFragile,
                IsPriority = isPriority
            };
        }


        [Fact]
        public void ClientSend_WhenSmallParcelSent_ThenDeliveryCreated()
        {
            // Arrange
            var service = CreateService();
            var parcel = CreateSmallParcel();

            // Act
            var result = service.ClientSend(parcel, 1, 2);

            // Assert
            Assert.True(result.DeliveryId > 0);
            Assert.NotEmpty(result.SecurityCode);
            Assert.True(result.Price > 0);
        }


        [Fact]
        public void GenerateSecurityCode_WhenCalled_ThenReturnsValidCode()
        {
            // Arrange
            var service = CreateService();

            // Act
            var code = service.GenerateSecurityCode();

            // Assert
            Assert.NotNull(code);
            Assert.Equal(6, code.Length);
            Assert.All(code, c => Assert.True(char.IsDigit(c)));
        }

        [Theory]
        [InlineData(40, 50, 60, 15)]
        [InlineData(90, 50, 150, 30)]
        [InlineData(300, 90, 250, 120)]

        public void CalculatePrice_WhenSendParcelAndDistanceThreeHundred_ThenReturnsPriceModifierTimesDistance(int parcelWidth, int parcelHeight, int parcelDepth, decimal expected)
        {
            // Arrange
            var service = CreateService();
            var parcel = new Parcel
            {
                Width = parcelWidth,
                Height = parcelHeight,
                Depth = parcelDepth,
                IsFragile = false,
                IsPriority = false
            };

            // Act
            var result = service.ClientSend(parcel, 1, 2);

            // Assert
            Assert.Equal(expected, result.Price);
        }

        [Fact]
        public void CalculatePrice_WhenSendPriorityParcel_ThenPriceIsDoubled()
        {
            // Arrange
            var service = CreateService();
            var standardParcel = CreateSmallParcel();
            var priorityParcel = CreateSmallParcel(isPriority: true);

            // Act
            var standardPrice = service.ClientSend(standardParcel, 1, 2).Price;
            var priorityPrice = service.ClientSend(priorityParcel, 1, 2).Price;

            // Assert
            Assert.Equal(2 * standardPrice, priorityPrice);
        }

        [Fact]
        public void CalculatePrice_WhenSendFragileParcel_ThenPriceIsThirtyPercentMore()
        {
            // Arrange
            var service = CreateService();
            var standardParcel = CreateSmallParcel();
            var priorityParcel = CreateSmallParcel(isFragile: true);

            // Act
            var standardPrice = service.ClientSend(standardParcel, 1, 2).Price;
            var priorityPrice = service.ClientSend(priorityParcel, 1, 2).Price;

            // Assert
            Assert.Equal(1.3m * standardPrice, priorityPrice);
        }


        [Fact]
        public void ClientSend_WhenSmallParcelFitsOnEmptySmallShelfAndSecondParcelIsSent_ThenFirstParcelTakesAllSmallShelfAndSecondOverflowsToMediumShelf()
        {
            // Arrange
            var service = CreateService();
            var firstParcel = new Parcel
            {
                Width = 50,
                Height = 60,
                Depth = 70,
                IsFragile = false,
                IsPriority = false
            };
            var secondParcel = new Parcel
            {
                Width = 1,
                Height = 1,
                Depth = 1,
                IsFragile = false,
                IsPriority = false
            };

            // Act
            var firstResult = service.ClientSend(firstParcel, 1, 2);
            var secondResult = service.ClientSend(secondParcel, 1, 2);

            // Assert
            Assert.True(firstResult.DeliveryId > 0);
            Assert.NotEmpty(firstResult.SecurityCode);
            Assert.Equal(15m, firstResult.Price);  
            Assert.True(secondResult.DeliveryId > 0);
            Assert.NotEmpty(secondResult.SecurityCode);
            //takes space in medium shelf, hence twice the price
            Assert.Equal(30m, secondResult.Price);
        }

        [Fact]
        public void ClientSend_WhenAllShelvesAreFull_ThenThrowsArgumentNullException()
        {
        // Arrange
            var service = CreateService();
            
            var smallFillingparcel = new Parcel
            {
                Width = ShelfSpace.SMALL_SHELF_WIDTH,
                Height = ShelfSpace.SMALL_SHELF_HEIGHT,
                Depth = ShelfSpace.SMALL_SHELF_DEPTH,
                IsFragile = false,
                IsPriority = false
            };
            var mediumFillingparcel = new Parcel
            {
                Width = ShelfSpace.MEDIUM_SHELF_WIDTH,
                Height = ShelfSpace.MEDIUM_SHELF_HEIGHT,
                Depth = ShelfSpace.MEDIUM_SHELF_DEPTH,
                IsFragile = false,
                IsPriority = false
            };
            var bigFillingparcel = new Parcel
            {
                Width = ShelfSpace.LARGE_SHELF_WIDTH,
                Height = ShelfSpace.LARGE_SHELF_HEIGHT,
                Depth = ShelfSpace.LARGE_SHELF_DEPTH,
                IsFragile = false,
                IsPriority = false
            };
            var lastParcel = new Parcel
            {
                Width = 1,
                Height = 1,
                Depth = 1,
                IsFragile = false,
                IsPriority = false
            };

            service.ClientSend(smallFillingparcel, 1, 2);
            service.ClientSend(mediumFillingparcel, 1, 2);
            service.ClientSend(bigFillingparcel, 1, 2);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.ClientSend(lastParcel, 1, 2));
        }



    }
}