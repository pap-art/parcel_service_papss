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
                Width = 40,
                Height = 50,
                Depth = 60,
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



    }
}