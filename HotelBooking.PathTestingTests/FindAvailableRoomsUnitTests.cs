namespace HotelBooking.PathTestingTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using HotelBooking.Core;
    using Moq;
    using Xunit;

    public class FindAvailableRoomsUnitTests
    {
        private readonly Mock<IRepository<Booking>> _mockBookingRepository;
        private readonly Mock<IRepository<Room>> _mockRoomRepository;
        private readonly IBookingManager _bookingManager;

        public FindAvailableRoomsUnitTests()
        {
            // Initialize Moq repositories
            _mockBookingRepository = new Mock<IRepository<Booking>>();
            _mockRoomRepository = new Mock<IRepository<Room>>();

            _bookingManager = new BookingManager(_mockBookingRepository.Object, _mockRoomRepository.Object);
        }

        [Fact]
        public async Task FindAvailableRoom_StartDateInPast_ThrowsArgumentException()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(-1);
            var endDate = DateTime.Today.AddDays(7);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _bookingManager.FindAvailableRoom(startDate, endDate));
            Assert.Equal("The start date cannot be in the past or later than the end date.", exception.Message);
        }

        [Fact]
        public async Task FindAvailableRoom_StartDateAfterEndDate_ThrowsArgumentException()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(10);
            var endDate = DateTime.Today.AddDays(5);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _bookingManager.FindAvailableRoom(startDate, endDate));
            Assert.Equal("The start date cannot be in the past or later than the end date.", exception.Message);
        }

        [Fact]
        public async Task FindAvailableRoom_AnAvailableRoomIsFound_ReturnsRoomId()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(6);
            var endDate = DateTime.Today.AddDays(10);
            var rooms = new List<Room> { new Room { Id = 1 }, new Room { Id = 2 } };
            var activeBookings = new List<Booking>
            {
                new Booking { RoomId = 1, StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(3), IsActive = true },
                new Booking { RoomId = 2, StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(12), IsActive = true }
            };

            _mockBookingRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(activeBookings);
            _mockRoomRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(rooms);

            // Act
            var result = await _bookingManager.FindAvailableRoom(startDate, endDate);

            // Assert
            Assert.Equal(1, result); // Room with Id 1 should be available
        }

        [Fact]
        public async Task FindAvailableRoom_NoAvailableRoom_ReturnsNegativeOne()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(1);
            var endDate = DateTime.Today.AddDays(5);
            var rooms = new List<Room> { new Room { Id = 1 }, new Room { Id = 2 } };
            var activeBookings = new List<Booking>
            {
                new Booking { RoomId = 1, StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(3), IsActive = true },
                new Booking { RoomId = 1, StartDate = DateTime.Today.AddDays(4), EndDate = DateTime.Today.AddDays(6), IsActive = true },
                new Booking { RoomId = 2, StartDate = DateTime.Today.AddDays(2), EndDate = DateTime.Today.AddDays(4), IsActive = true },
                new Booking { RoomId = 2, StartDate = DateTime.Today.AddDays(5), EndDate = DateTime.Today.AddDays(7), IsActive = true }
            };

            _mockBookingRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(activeBookings);
            _mockRoomRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(rooms);

            // Act
            var result = await _bookingManager.FindAvailableRoom(startDate, endDate);

            // Assert
            Assert.Equal(-1, result);
        }
    }
}
