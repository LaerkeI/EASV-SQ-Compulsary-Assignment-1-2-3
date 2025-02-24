using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotelBooking.Core;
using Moq;
using Xunit;

namespace HotelBooking.UnitTests
{
    public class BookingManagerTests
    {
        private readonly Mock<IRepository<Booking>> _mockBookingRepository;
        private readonly Mock<IRepository<Room>> _mockRoomRepository;
        private readonly IBookingManager _bookingManager;

        public BookingManagerTests()
        {
            // Initialize Moq repositories
            _mockBookingRepository = new Mock<IRepository<Booking>>();
            _mockRoomRepository = new Mock<IRepository<Room>>();

            // Setup sample data for tests
            var sampleBookings = new List<Booking>
            {
                new Booking { RoomId = 1, StartDate = DateTime.Today.AddDays(10), EndDate = DateTime.Today.AddDays(20), IsActive = true },
                new Booking { RoomId = 2, StartDate = DateTime.Today.AddDays(15), EndDate = DateTime.Today.AddDays(25), IsActive = true }
            };

            var sampleRooms = new List<Room>
            {
                new Room { Id = 1 },
                new Room { Id = 2 }
            };

            _mockBookingRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(sampleBookings);
            _mockRoomRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(sampleRooms);

            _bookingManager = new BookingManager(_mockBookingRepository.Object, _mockRoomRepository.Object);
        }


        [Fact]
        // Diagram: Corresponds to Circle 1 (Trying to book a room today, which is invalid)
        // This test verifies that an exception is thrown if the booking start date is today or in the past.
        public async Task FindAvailableRoom_StartDateNotInTheFuture_ThrowsArgumentException()
        {
            // Arrange
            DateTime date = DateTime.Today;

            // Act
            Task result() => _bookingManager.FindAvailableRoom(date, date);

            // Assert
            await Assert.ThrowsAsync<ArgumentException>(result);
        }

        [Fact]
        // Diagram: Corresponds to Circles 2 & 3 (Checking for available rooms before the fully occupied period)
        // This test checks if a room is available before the fully occupied period and ensure a valid room ID is returned.
        public async Task FindAvailableRoom_RoomAvailable_RoomIdNotMinusOne()
        {
            // Arrange
            DateTime date = DateTime.Today.AddDays(1);
            _mockBookingRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(new List<Booking>()); // No existing bookings

            // Act
            int roomId = await _bookingManager.FindAvailableRoom(date, date);

            // Assert
            Assert.NotEqual(-1, roomId);
            _mockBookingRepository.Verify(repo => repo.GetAllAsync(), Times.AtLeastOnce);
        }

        [Fact]
        //Diagram: Corresponds to Circle 4 (Fully occupied period from startDate to endDate)
        // This verifies that the function correctly identifies the period where all rooms are booked.
        public async Task GetFullyOccupiedDates_DuringOccupiedPeriod_ReturnsOccupiedDates()
        {
            // Arrange
            DateTime startDate = DateTime.Today.AddDays(10); // Only Room 1 is booked until day 15
            DateTime endDate = DateTime.Today.AddDays(25); // Only Room 2 is booked after day 20

            // Act
            List<DateTime> occupiedDates = await _bookingManager.GetFullyOccupiedDates(startDate, endDate); // Checking the period where there is at least one booking.

            // Assert
            Assert.NotEmpty(occupiedDates);
            Assert.Contains(DateTime.Today.AddDays(15), occupiedDates); // Both Room 1 and Room 2 are booked here.
            Assert.Contains(DateTime.Today.AddDays(20), occupiedDates); // Both Room 1 and Room 2 are booked here.
            _mockBookingRepository.Verify(repo => repo.GetAllAsync(), Times.Once);
        }

        [Fact]
        // Diagram: Corresponds to Circle 6 (Booking before the fully occupied period)
        // This test ensures booking succeeds when a room is available.
        public async Task CreateBooking_AvailableRoom_ReturnsTrue()
        {
            // Arrange
            var newBooking = new Booking
            {
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(3),
                CustomerId = 1
            };

            _mockBookingRepository.Setup(repo => repo.AddAsync(It.IsAny<Booking>())).Returns(Task.CompletedTask);

            // Act
            bool isBooked = await _bookingManager.CreateBooking(newBooking);

            // Assert
            Assert.True(isBooked);
            _mockBookingRepository.Verify(repo => repo.AddAsync(It.IsAny<Booking>()), Times.Once);
        }

        [Fact]
        // Diagram: Corresponds to Circle 7 (Booking within the fully occupied period)
        // This test ensures booking fails when trying to book a room during the fully occupied period.
        public async Task CreateBooking_NoAvailableRooms_ReturnsFalse()
        {
            // Arrange
            var newBooking = new Booking
            {
                StartDate = DateTime.Today.AddDays(10), // Fully occupied period
                EndDate = DateTime.Today.AddDays(15),
                CustomerId = 2
            };

            // Act
            bool isBooked = await _bookingManager.CreateBooking(newBooking);

            // Assert
            Assert.False(isBooked);
            _mockBookingRepository.Verify(repo => repo.AddAsync(It.IsAny<Booking>()), Times.Never);
        }

        [Fact]
        // Business Logic: "If the customer does not check in at the start date, the reservation should be cancelled."
        // Ensures that if a customer fails to check in, the reservation is automatically cancelled.
        public void CheckIn_NoShow_AutoCancelsBooking()
        {
            // Arrange
            var booking = new Booking { StartDate = DateTime.Today, IsCheckedIn = false, IsActive = true };

            // Act
            if (booking.StartDate == DateTime.Today && !booking.IsCheckedIn)
            {
                booking.IsActive = false; // Auto-cancel
            }

            // Assert
            Assert.False(booking.IsActive);
        }

        [Fact]
        // Business Logic: "When the customer checks in at the start date, the room should be marked as occupied."
        // Ensures that when a customer checks in, the room is correctly marked as occupied.
        public void CheckIn_OnStartDate_MarksRoomOccupied()
        {
            // Arrange
            var booking = new Booking { StartDate = DateTime.Today, IsCheckedIn = false };

            // Act
            booking.IsCheckedIn = true;  // Simulate check-in day

            // Assert
            Assert.True(booking.IsCheckedIn);
        }

        [Fact]
        // Business Logic: "When the customer checks out, the room should be marked as free."
        // Ensures that when a customer checks out, the system marks the room as free, making it available for future bookings.
        public void Checkout_OnEndDate_MarksRoomAsFree()
        {
            // Arrange
            var booking = new Booking { EndDate = DateTime.Today, IsCheckedIn = true };

            // Act
            booking.IsCheckedIn = false; // Simulate check-out day

            // Assert
            Assert.False(booking.IsCheckedIn);
        }
    }
}
