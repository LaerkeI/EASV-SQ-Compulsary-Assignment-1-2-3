Feature: Create Booking
  As a customer
  I want to book a hotel room
  So that I can stay during my chosen dates

Background:
    Given the hotel has rooms available 

@mytag
Scenario: Successful booking and check-in
    Given a room is available for the requested dates
    When I make a booking from "2025-06-01" to "2025-06-05"
    And I check in on "2025-06-01"
    Then the booking should be confirmed
    And the room should be marked as occupied

Scenario: Customer cancels the booking before the start date
    Given a room is available for the requested dates
    When I make a booking from "2025-06-01" to "2025-06-05"
    And I cancel the booking before "2025-06-01"
    Then the booking should be canceled
    And the room should be available

Scenario: Booking is canceled due to no-show
    Given a room is available for the requested dates
    When I make a booking from "2025-06-01" to "2025-06-05"
    And I do not check in on "2025-06-01"
    Then the booking should be canceled
    And the room should be available

Scenario: Booking is rejected if the room is unavailable
    Given no rooms are available for the requested dates
    When I attempt to book a room from "2025-06-01" to "2025-06-05"
    Then the booking should be rejected