using System;
using System.Linq;
using ParcelService.Enums;
using ParcelService.Exceptions;
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
                ParcelLockers = new[] {
                    CreateTestLocker(1, 100, new [] {
                        (1, ParcelShelfType.Small),
                        (2, ParcelShelfType.Medium),
                        (3, ParcelShelfType.Large)
                      }),
                    CreateTestLocker(2, 200, new [] {
                        (4, ParcelShelfType.Small),
                        (5, ParcelShelfType.Medium),
                        (6, ParcelShelfType.Large)
                    }),
                    CreateTestLocker(3, 300, new [] {
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
                Width = 200,
                Height = 90,
                Depth = 200,
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
        public void ClientSend_WhenAllShelvesAreFull_ThenThrowsNoSpaceInLockerException()
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
            Assert.Throws<NoSpaceInLockerException>(() => service.ClientSend(lastParcel, 1, 2));
        }

        [Fact]
        public void PickUpParcelsFromLocker_WithParcelsWaiting_ShouldReturnDeliveryIds()
        {
            // Arrange
            var service = CreateService();
            var parcel1 = CreateSmallParcel();
            var parcel2 = CreateMediumParcel();

            var send1 = service.ClientSend(parcel1, 1, 2);
            var send2 = service.ClientSend(parcel2, 1, 3);

            // Act
            var pickedUpIds = service.PickUpParcelsFromLocker(1);

            // Assert
            Assert.Equal(2, pickedUpIds.Length);
            Assert.Contains(send1.DeliveryId, pickedUpIds);
            Assert.Contains(send2.DeliveryId, pickedUpIds);
        }

        [Fact]
        public void PickUpParcelsFromLocker_WithPriorityParcels_ShouldPickupPriorityFirst()
        {
            // Arrange
            var service = CreateService();
            var normalParcel = CreateMediumParcel();
            var priorityParcel = CreateMediumParcel(isPriority: true);

            var send1 = service.ClientSend(normalParcel, 1, 2);
            var send2 = service.ClientSend(priorityParcel, 1, 3);

            // Act
            var pickedUpIds = service.PickUpParcelsFromLocker(1);

            // Assert
            Assert.Contains(send2.DeliveryId, pickedUpIds); // Priority should always be included
        }

        [Fact]
        public void PickUpParcelsFromLocker_WithNoWaitingParcels_ShouldReturnEmptyArray()
        {
            // Arrange
            var service = CreateService();

            // Act
            var pickedUpIds = service.PickUpParcelsFromLocker(1);

            // Assert
            Assert.Empty(pickedUpIds);
        }

        [Fact]
        public void PickUpParcels_WhenParcelVolumeBiggerThanTransport_ThenDoNotPickUpAllParcels()
        {
            // Arrange
            var service = new ParcelServiceImpl
            {
                CompanyName = "3 Medium Shelves Co",
                ParcelLockers = new[] {
                    CreateTestLocker(1, 100, new [] {
                        (1, ParcelShelfType.Large),
                        (2, ParcelShelfType.Large),
                        (3, ParcelShelfType.Large)
                    }),
                    CreateTestLocker(2, 200, new [] {
                        (4, ParcelShelfType.Large),
                        (5, ParcelShelfType.Large),
                        (6, ParcelShelfType.Large)
                    }),
                    CreateTestLocker(3, 300, new [] {
                        (7, ParcelShelfType.Large),
                        (8, ParcelShelfType.Large),
                        (9, ParcelShelfType.Large)
                    })
                }
            };

            // Create parcels that collectively exceed vehicle capacity (800x200x200)
            var parcel1 = new Parcel
            {
                Width = 300,
                Height = 100,
                Depth = 100,
                IsFragile = false,
                IsPriority = false
            };
            var parcel2 = new Parcel
            {
                Width = 300,
                Height = 100,
                Depth = 100,
                IsFragile = false,
                IsPriority = false
            };
            var parcel3 = new Parcel
            {
                Width = 300,
                Height = 100,
                Depth = 100,
                IsFragile = false,
                IsPriority = false
            };

            // Send all three parcels
            var send1 = service.ClientSend(parcel1, 1, 2);
            var send2 = service.ClientSend(parcel2, 1, 2);
            var send3 = service.ClientSend(parcel3, 1, 2);

            // Act
            var pickedUpIds = service.PickUpParcelsFromLocker(1);

            // Assert
            Assert.Equal(2, pickedUpIds.Length); // Should pick up only 2 of the 3 parcels
        }

        [Fact]
        public void PickUpParcels_WhenParcelVolumeBiggerThanTransport_ThenGivePriorityFirst()
        {
            // Arrange
            var service = new ParcelServiceImpl
            {
                CompanyName = "3 Medium Shelves Co",
                ParcelLockers = new[] {
                CreateTestLocker(1, 100, new [] {
                    (1, ParcelShelfType.Large),
                    (2, ParcelShelfType.Large),
                    (3, ParcelShelfType.Large),
                    (4, ParcelShelfType.Large)
                }),
                CreateTestLocker(2, 200, new [] {
                    (5, ParcelShelfType.Large),
                    (6, ParcelShelfType.Large),
                    (7, ParcelShelfType.Large),
                    (8, ParcelShelfType.Large)
                }),
                CreateTestLocker(3, 300, new [] {
                    (9, ParcelShelfType.Large),
                    (10, ParcelShelfType.Large),
                    (11, ParcelShelfType.Large),
                    (12, ParcelShelfType.Large)
                })
              }
            };

            // Create parcels that collectively exceed vehicle capacity (800x200x200)
            var parcel1 = new Parcel
            {
                Width = 300,
                Height = 100,
                Depth = 100,
                IsFragile = false,
                IsPriority = false
            };
            var parcel2 = new Parcel
            {
                Width = 300,
                Height = 100,
                Depth = 100,
                IsFragile = false,
                IsPriority = false
            };
            var priorityParcel1 = new Parcel
            {
                Width = 300,
                Height = 100,
                Depth = 100,
                IsFragile = false,
                IsPriority = true
            };
            var priorityParcel2 = new Parcel
            {
                Width = 300,
                Height = 100,
                Depth = 100,
                IsFragile = false,
                IsPriority = true
            };

            // Send all three parcels
            var send1 = service.ClientSend(parcel1, 1, 2);
            var send2 = service.ClientSend(parcel2, 1, 2);
            var send3 = service.ClientSend(priorityParcel1, 1, 2);
            var send4 = service.ClientSend(priorityParcel2, 1, 2);

            // Act
            var pickedUpIds = service.PickUpParcelsFromLocker(1);

            // Assert
            Assert.Equal(2, pickedUpIds.Length);
            Assert.Contains(send3.DeliveryId, pickedUpIds);
            Assert.Contains(send4.DeliveryId, pickedUpIds);
        }

        [Fact]
        public void DeliverParcels_WithParcelsAtBase_ThenDeliverToLocker()
        {
            // Arrange
            var service = CreateService();
            var parcel1 = CreateSmallParcel();
            var parcel2 = CreateMediumParcel();

            var send1 = service.ClientSend(parcel1, 1, 2);
            var send2 = service.ClientSend(parcel2, 1, 2);

            // First pickup the parcels
            service.PickUpParcelsFromLocker(1);

            // Act
            var deliveredIds = service.DeliverParcels(2);

            // Assert
            Assert.Equal(2, deliveredIds.Length);
            Assert.Contains(send1.DeliveryId, deliveredIds);
            Assert.Contains(send2.DeliveryId, deliveredIds);
        }

        [Fact]
        public void ClientReceive_WithValidDelivery_ShouldReturnShelfId()
        {
            // Arrange
            var service = CreateService();
            var parcel = CreateSmallParcel();
            var sendResult = service.ClientSend(parcel, 1, 2);

            // Simulate delivery process
            service.PickUpParcelsFromLocker(1);
            service.DeliverParcels(2);

            // Act
            int shelfId = service.ClientReceive(sendResult.DeliveryId, sendResult.SecurityCode);

            // Assert
            Assert.True(shelfId > 0);
        }

        [Fact]
        public void ClientReceive_WithInvalidSecurityCode_ShouldThrowException()
        {
            // Arrange
            var service = CreateService();
            var parcel = CreateSmallParcel();
            var sendResult = service.ClientSend(parcel, 1, 2);

            service.PickUpParcelsFromLocker(1);
            service.DeliverParcels(2);

            // Act & Assert
            Assert.Throws<InvalidSecurityCodeException>(() =>
              service.ClientReceive(sendResult.DeliveryId, "INVALID"));
        }

        [Fact]
        public void ClientReceive_WithNotYetDeliveredParcel_ShouldThrowException()
        {
            // Arrange
            var service = CreateService();
            var parcel = CreateSmallParcel();
            var sendResult = service.ClientSend(parcel, 1, 2);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
              service.ClientReceive(sendResult.DeliveryId, sendResult.SecurityCode));
        }

        [Fact]
        public void GetMonthlyIncomeReport_WithDeliveries_ShouldReturnCorrectIncome()
        {
            // Arrange
            var service = CreateService();
            var parcel1 = CreateSmallParcel();
            var parcel2 = CreateMediumParcel(isPriority: true);

            var send1 = service.ClientSend(parcel1, 1, 2);
            var send2 = service.ClientSend(parcel2, 1, 3);

            decimal expectedIncome = send1.Price + send2.Price;

            // Act
            var report = service.GetMonthlyIncomeReport();

            // Assert
            Assert.Equal(DateTime.Now.Year, report.Year);
            Assert.Equal(DateTime.Now.Month, report.Month);
            Assert.Equal(expectedIncome, report.Income);
        }

        [Fact]
        public void GetAverageDeliveryTime_WithCompletedDeliveries_ShouldCalculateAverage()
        {
            // Arrange
            var service = CreateService();
            var parcel = CreateSmallParcel();

            var sendResult = service.ClientSend(parcel, 1, 2);

            service.PickUpParcelsFromLocker(1);
            service.DeliverParcels(2);
            service.ClientReceive(sendResult.DeliveryId, sendResult.SecurityCode);

            var periodStart = DateTimeOffset.Now.AddDays(-1);
            var periodEnd = DateTimeOffset.Now.AddDays(1);

            // Act
            var averageTime = service.GetAverageDeliveryTime(periodStart, periodEnd);

            // Assert
            Assert.True(averageTime.TotalSeconds > 0);
        }
    }
}