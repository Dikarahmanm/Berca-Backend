using Berca_Backend.DTOs;
using Berca_Backend.Models;

namespace Berca_Backend.Services.Interfaces
{
    /// <summary>
    /// Service interface for calendar event management
    /// Handles business event scheduling, reminders, and auto-generation
    /// Indonesian business context with timezone support
    /// </summary>
    public interface ICalendarEventService
    {
        // ==================== CALENDAR EVENT CRUD ==================== //

        /// <summary>
        /// Get calendar events with filtering and pagination
        /// </summary>
        /// <param name="queryParams">Query parameters for filtering</param>
        /// <param name="requestingUserId">ID of user making request</param>
        /// <returns>Paginated calendar events</returns>
        Task<CalendarEventPagedResponseDto> GetEventsAsync(CalendarEventQueryParams queryParams, int requestingUserId);

        /// <summary>
        /// Get calendar events for specific date range
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <param name="branchId">Optional branch filter</param>
        /// <param name="requestingUserId">ID of user making request</param>
        /// <returns>List of events in date range</returns>
        Task<List<CalendarEventDto>> GetEventsByDateRangeAsync(DateTime startDate, DateTime endDate, int? branchId, int requestingUserId);

        /// <summary>
        /// Get single calendar event by ID
        /// </summary>
        /// <param name="eventId">Event ID</param>
        /// <param name="requestingUserId">ID of user making request</param>
        /// <returns>Event details or null if not found</returns>
        Task<CalendarEventDto?> GetEventByIdAsync(int eventId, int requestingUserId);

        /// <summary>
        /// Create new calendar event
        /// </summary>
        /// <param name="createDto">Event creation data</param>
        /// <param name="createdBy">ID of user creating event</param>
        /// <returns>Created event details</returns>
        Task<CalendarEventDto> CreateEventAsync(CreateCalendarEventDto createDto, int createdBy);

        /// <summary>
        /// Update existing calendar event
        /// </summary>
        /// <param name="eventId">Event ID to update</param>
        /// <param name="updateDto">Updated event data</param>
        /// <param name="updatedBy">ID of user updating event</param>
        /// <returns>Updated event details or null if not found</returns>
        Task<CalendarEventDto?> UpdateEventAsync(int eventId, UpdateCalendarEventDto updateDto, int updatedBy);

        /// <summary>
        /// Delete calendar event
        /// </summary>
        /// <param name="eventId">Event ID to delete</param>
        /// <param name="deletedBy">ID of user deleting event</param>
        /// <returns>True if deleted successfully</returns>
        Task<bool> DeleteEventAsync(int eventId, int deletedBy);

        /// <summary>
        /// Bulk operations on calendar events
        /// </summary>
        /// <param name="operationDto">Bulk operation details</param>
        /// <param name="requestingUserId">ID of user performing operation</param>
        /// <returns>Number of events affected</returns>
        Task<int> BulkOperationAsync(BulkEventOperationDto operationDto, int requestingUserId);

        // ==================== CALENDAR VIEWS ==================== //

        /// <summary>
        /// Get calendar month view with events
        /// </summary>
        /// <param name="year">Year</param>
        /// <param name="month">Month (1-12)</param>
        /// <param name="branchId">Optional branch filter</param>
        /// <param name="requestingUserId">ID of user making request</param>
        /// <returns>Month view with events</returns>
        Task<CalendarMonthViewDto> GetMonthViewAsync(int year, int month, int? branchId, int requestingUserId);

        /// <summary>
        /// Get daily events summary
        /// </summary>
        /// <param name="date">Date to get summary for</param>
        /// <param name="branchId">Optional branch filter</param>
        /// <param name="requestingUserId">ID of user making request</param>
        /// <returns>Daily events summary</returns>
        Task<DailyEventsSummaryDto> GetDailySummaryAsync(DateTime date, int? branchId, int requestingUserId);

        /// <summary>
        /// Get calendar dashboard data
        /// </summary>
        /// <param name="requestingUserId">ID of user making request</param>
        /// <param name="branchId">Optional branch filter</param>
        /// <returns>Calendar dashboard summary</returns>
        Task<CalendarDashboardDto> GetDashboardAsync(int requestingUserId, int? branchId = null);

        // ==================== BUSINESS INTEGRATION ==================== //

        /// <summary>
        /// Auto-generate events from business data
        /// </summary>
        /// <param name="generateDto">Auto-generation parameters</param>
        /// <param name="requestingUserId">ID of user requesting generation</param>
        /// <returns>Number of events generated</returns>
        Task<int> AutoGenerateEventsAsync(AutoGenerateEventsDto generateDto, int requestingUserId);

        /// <summary>
        /// Generate product expiry events
        /// </summary>
        /// <param name="branchId">Optional branch filter</param>
        /// <param name="daysAhead">Days ahead to generate events for</param>
        /// <param name="requestingUserId">ID of user requesting generation</param>
        /// <returns>Number of events generated</returns>
        Task<int> GenerateProductExpiryEventsAsync(int? branchId, int daysAhead, int requestingUserId);

        /// <summary>
        /// Generate facture due date events
        /// </summary>
        /// <param name="branchId">Optional branch filter</param>
        /// <param name="daysAhead">Days ahead to generate events for</param>
        /// <param name="requestingUserId">ID of user requesting generation</param>
        /// <returns>Number of events generated</returns>
        Task<int> GenerateFactureDueEventsAsync(int? branchId, int daysAhead, int requestingUserId);

        /// <summary>
        /// Generate member payment reminder events
        /// </summary>
        /// <param name="branchId">Optional branch filter</param>
        /// <param name="daysAhead">Days ahead to generate events for</param>
        /// <param name="requestingUserId">ID of user requesting generation</param>
        /// <returns>Number of events generated</returns>
        Task<int> GenerateMemberPaymentEventsAsync(int? branchId, int daysAhead, int requestingUserId);

        /// <summary>
        /// Get events related to specific entity
        /// </summary>
        /// <param name="entityType">Entity type (Product, Facture, Member)</param>
        /// <param name="entityId">Entity ID</param>
        /// <param name="requestingUserId">ID of user making request</param>
        /// <returns>List of related events</returns>
        Task<List<CalendarEventDto>> GetEventsForEntityAsync(string entityType, int entityId, int requestingUserId);

        // ==================== REMINDER MANAGEMENT ==================== //

        /// <summary>
        /// Get upcoming reminders that need to be sent
        /// </summary>
        /// <param name="beforeTime">Get reminders scheduled before this time</param>
        /// <returns>List of reminder details</returns>
        Task<List<EventReminderDto>> GetUpcomingRemindersAsync(DateTime beforeTime);

        /// <summary>
        /// Mark reminder as sent
        /// </summary>
        /// <param name="reminderId">Reminder ID</param>
        /// <param name="sentTime">Time when reminder was sent</param>
        /// <param name="success">Whether reminder was sent successfully</param>
        /// <param name="errorMessage">Error message if failed</param>
        /// <returns>True if marked successfully</returns>
        Task<bool> MarkReminderSentAsync(int reminderId, DateTime sentTime, bool success, string? errorMessage = null);

        /// <summary>
        /// Create reminders for events that need them
        /// </summary>
        /// <param name="eventId">Event ID to create reminders for</param>
        /// <returns>Number of reminders created</returns>
        Task<int> CreateEventRemindersAsync(int eventId);

        /// <summary>
        /// Get pending reminders for specific user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="branchId">Optional branch filter</param>
        /// <returns>List of pending reminders</returns>
        Task<List<EventReminderDto>> GetUserPendingRemindersAsync(int userId, int? branchId = null);

        // ==================== STATISTICS & ANALYTICS ==================== //

        /// <summary>
        /// Get calendar event statistics
        /// </summary>
        /// <param name="requestingUserId">ID of user making request</param>
        /// <param name="branchId">Optional branch filter</param>
        /// <param name="fromDate">Start date for statistics</param>
        /// <param name="toDate">End date for statistics</param>
        /// <returns>Event statistics</returns>
        Task<CalendarEventStatsDto> GetEventStatisticsAsync(int requestingUserId, int? branchId = null, DateTime? fromDate = null, DateTime? toDate = null);

        /// <summary>
        /// Get events requiring attention (overdue, high priority, etc.)
        /// </summary>
        /// <param name="requestingUserId">ID of user making request</param>
        /// <param name="branchId">Optional branch filter</param>
        /// <returns>List of events requiring attention</returns>
        Task<List<CalendarEventDto>> GetEventsRequiringAttentionAsync(int requestingUserId, int? branchId = null);

        /// <summary>
        /// Get upcoming events for user
        /// </summary>
        /// <param name="requestingUserId">ID of user making request</param>
        /// <param name="days">Number of days ahead to look</param>
        /// <param name="branchId">Optional branch filter</param>
        /// <returns>List of upcoming events</returns>
        Task<List<CalendarEventDto>> GetUpcomingEventsAsync(int requestingUserId, int days = 7, int? branchId = null);

        // ==================== VALIDATION & UTILITIES ==================== //

        /// <summary>
        /// Validate event data for creation/update
        /// </summary>
        /// <param name="createDto">Event data to validate</param>
        /// <returns>List of validation errors (empty if valid)</returns>
        Task<List<string>> ValidateEventDataAsync(CreateCalendarEventDto createDto);

        /// <summary>
        /// Check if user can access event
        /// </summary>
        /// <param name="eventId">Event ID</param>
        /// <param name="userId">User ID</param>
        /// <returns>True if user can access event</returns>
        Task<bool> CanUserAccessEventAsync(int eventId, int userId);

        /// <summary>
        /// Clean up old events and reminders
        /// </summary>
        /// <param name="olderThanDays">Remove events older than this many days</param>
        /// <returns>Number of events cleaned up</returns>
        Task<int> CleanupOldEventsAsync(int olderThanDays);

        /// <summary>
        /// Search events by text
        /// </summary>
        /// <param name="searchTerm">Search term</param>
        /// <param name="requestingUserId">ID of user making request</param>
        /// <param name="branchId">Optional branch filter</param>
        /// <param name="maxResults">Maximum results to return</param>
        /// <returns>List of matching events</returns>
        Task<List<CalendarEventDto>> SearchEventsAsync(string searchTerm, int requestingUserId, int? branchId = null, int maxResults = 20);
    }
}