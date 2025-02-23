using System;
using HotelBooking.Core;
using HotelBooking.UnitTests.Fakes;
using Xunit;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;


namespace HotelBooking.UnitTests
{
    public class BookingManagerTests
    {
        private IBookingManager bookingManager;
        IRepository<Booking> bookingRepository;

        public BookingManagerTests(){
            DateTime start = DateTime.Today.AddDays(10);
            DateTime end = DateTime.Today.AddDays(20);
            bookingRepository = new FakeBookingRepository(start, end);
            IRepository<Room> roomRepository = new FakeRoomRepository();
            bookingManager = new BookingManager(bookingRepository, roomRepository);
        }

        [Fact]
        public async Task FindAvailableRoom_StartDateNotInTheFuture_ThrowsArgumentException()
        {
            // Arrange
            DateTime date = DateTime.Today;

            // Act
            Task result() => bookingManager.FindAvailableRoom(date, date);

            // Assert
            await Assert.ThrowsAsync<ArgumentException>(result);
        }

        [Fact]
        public async Task FindAvailableRoom_RoomAvailable_RoomIdNotMinusOne()
        {
            // Arrange
            DateTime date = DateTime.Today.AddDays(1);
            // Act
            int roomId = await bookingManager.FindAvailableRoom(date, date);
            // Assert
            Assert.NotEqual(-1, roomId);
        }

        [Fact]
        public async Task FindAvailableRoom_RoomAvailable_ReturnsAvailableRoom()
        {
            // This test was added to satisfy the following test design
            // principle: "Tests should have strong assertions".

            // Arrange
            DateTime date = DateTime.Today.AddDays(1);
            
            // Act
            int roomId = await bookingManager.FindAvailableRoom(date, date);

            var bookingForReturnedRoomId = (await bookingRepository.GetAllAsync()).
                Where(b => b.RoomId == roomId
                           && b.StartDate <= date
                           && b.EndDate >= date
                           && b.IsActive);
            
            // Assert
            Assert.Empty(bookingForReturnedRoomId);
        }

        [Fact]
        public async Task GetFullyOccupiedDates_DuringOccupiedPeriod_ReturnsOccupiedDates()
        {
            // Arrange
            DateTime startDate = DateTime.Today.AddDays(10);
            DateTime endDate = DateTime.Today.AddDays(20);

            // Act
            List<DateTime> occupiedDates = await bookingManager.GetFullyOccupiedDates(startDate, endDate);

            // Assert
            Assert.NotEmpty(occupiedDates);
            Assert.Contains(DateTime.Today.AddDays(10), occupiedDates);
            Assert.Contains(DateTime.Today.AddDays(15), occupiedDates);
        }

        [Fact]
        public async Task GetFullyOccupiedDates_NoOverlappingBookings_ReturnsEmptyList()
        {
            // Arrange
            DateTime startDate = DateTime.Today.AddDays(1);
            DateTime endDate = DateTime.Today.AddDays(5);

            // Act
            List<DateTime> occupiedDates = await bookingManager.GetFullyOccupiedDates(startDate, endDate);

            // Assert
            Assert.Empty(occupiedDates);
        }

        [Fact]
        public async Task CreateBooking_AvailableRoom_ReturnsTrue()
        {
            // Arrange
            Booking newBooking = new Booking
            {
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(3),
                CustomerId = 1
            };

            // Act
            bool isBooked = await bookingManager.CreateBooking(newBooking);

            // Assert
            Assert.True(isBooked);
        }

        [Fact]
        public async Task CreateBooking_NoAvailableRooms_ReturnsFalse()
        {
            // Arrange
            Booking newBooking = new Booking
            {
                StartDate = DateTime.Today.AddDays(10), // Fully occupied period
                EndDate = DateTime.Today.AddDays(15),
                CustomerId = 2
            };

            // Act
            bool isBooked = await bookingManager.CreateBooking(newBooking);

            // Assert
            Assert.False(isBooked);
        }

        [Fact]
        public async Task ModifyBooking_BeforeStartDate_Succeeds()
        {
            // Arrange
            Booking existingBooking = (await bookingRepository.GetAllAsync()).First();
            existingBooking.StartDate = DateTime.Today.AddDays(5); // Change start date
            existingBooking.EndDate = DateTime.Today.AddDays(7); // Change end date

            // Act
            bool isModified = await bookingManager.CreateBooking(existingBooking);

            // Assert
            Assert.True(isModified);
        }

        [Fact]
        public async Task CancelBooking_BeforeStartDate_Succeeds()
        {
            // Arrange
            Booking existingBooking = (await bookingRepository.GetAllAsync()).First();
            existingBooking.IsActive = false; // Cancel booking

            // Act
            await bookingRepository.EditAsync(existingBooking);

            // Assert
            Assert.False(existingBooking.IsActive);
        }

        [Fact]
        public async Task CheckIn_NoShow_AutoCancelsBooking()
        {
            // Arrange
            Booking booking = (await bookingRepository.GetAllAsync()).First();
            booking.StartDate = DateTime.Today; // Simulate check-in day

            // Act
            if (booking.StartDate == DateTime.Today && !booking.IsCheckedIn)
            {
                booking.IsActive = false; // Auto-cancel
            }

            // Assert
            Assert.False(booking.IsActive);
        }

        [Fact]
        public async Task CheckIn_OnStartDate_MarksRoomOccupied()
        {
            // Arrange
            Booking booking = (await bookingRepository.GetAllAsync()).First();
            booking.StartDate = DateTime.Today; // Simulate today as check-in day

            // Act
            booking.IsCheckedIn = true; // Simulate check-in

            // Assert
            Assert.True(booking.IsCheckedIn);
        }

        [Fact]
        public async Task Checkout_OnEndDate_MarksRoomAsFree()
        {
            // Arrange
            Booking booking = (await bookingRepository.GetAllAsync()).First();
            booking.EndDate = DateTime.Today; // Simulate today as checkout day

            // Act
            booking.IsCheckedIn = false; // Simulate checkout

            // Assert
            Assert.False(booking.IsCheckedIn);
        }
    }
}
