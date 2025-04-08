using System;
using System.Collections.Generic;

namespace Adept.Services.Calendar
{
    /// <summary>
    /// Helper class for generating recurrence rules for calendar events
    /// </summary>
    public static class RecurrenceRuleGenerator
    {
        /// <summary>
        /// Frequency types for recurrence rules
        /// </summary>
        public enum FrequencyType
        {
            /// <summary>
            /// Daily recurrence
            /// </summary>
            Daily,

            /// <summary>
            /// Weekly recurrence
            /// </summary>
            Weekly,

            /// <summary>
            /// Monthly recurrence
            /// </summary>
            Monthly,

            /// <summary>
            /// Yearly recurrence
            /// </summary>
            Yearly
        }

        /// <summary>
        /// Days of the week
        /// </summary>
        [Flags]
        public enum DaysOfWeek
        {
            /// <summary>
            /// No days
            /// </summary>
            None = 0,

            /// <summary>
            /// Monday
            /// </summary>
            Monday = 1,

            /// <summary>
            /// Tuesday
            /// </summary>
            Tuesday = 2,

            /// <summary>
            /// Wednesday
            /// </summary>
            Wednesday = 4,

            /// <summary>
            /// Thursday
            /// </summary>
            Thursday = 8,

            /// <summary>
            /// Friday
            /// </summary>
            Friday = 16,

            /// <summary>
            /// Saturday
            /// </summary>
            Saturday = 32,

            /// <summary>
            /// Sunday
            /// </summary>
            Sunday = 64,

            /// <summary>
            /// Weekdays (Monday through Friday)
            /// </summary>
            Weekdays = Monday | Tuesday | Wednesday | Thursday | Friday,

            /// <summary>
            /// Weekend days (Saturday and Sunday)
            /// </summary>
            Weekend = Saturday | Sunday,

            /// <summary>
            /// All days of the week
            /// </summary>
            All = Weekdays | Weekend
        }

        /// <summary>
        /// Creates a daily recurrence rule
        /// </summary>
        /// <param name="interval">The interval between occurrences (e.g., every 2 days)</param>
        /// <param name="count">The number of occurrences (null for indefinite)</param>
        /// <param name="until">The end date (null for indefinite)</param>
        /// <returns>The recurrence rule</returns>
        public static string CreateDailyRule(int interval = 1, int? count = null, DateTime? until = null)
        {
            return CreateRule(FrequencyType.Daily, interval, count, until);
        }

        /// <summary>
        /// Creates a weekly recurrence rule
        /// </summary>
        /// <param name="daysOfWeek">The days of the week for the recurrence</param>
        /// <param name="interval">The interval between occurrences (e.g., every 2 weeks)</param>
        /// <param name="count">The number of occurrences (null for indefinite)</param>
        /// <param name="until">The end date (null for indefinite)</param>
        /// <returns>The recurrence rule</returns>
        public static string CreateWeeklyRule(DaysOfWeek daysOfWeek, int interval = 1, int? count = null, DateTime? until = null)
        {
            var rule = CreateRule(FrequencyType.Weekly, interval, count, until);

            if (daysOfWeek != DaysOfWeek.None)
            {
                var days = new List<string>();

                if ((daysOfWeek & DaysOfWeek.Monday) == DaysOfWeek.Monday) days.Add("MO");
                if ((daysOfWeek & DaysOfWeek.Tuesday) == DaysOfWeek.Tuesday) days.Add("TU");
                if ((daysOfWeek & DaysOfWeek.Wednesday) == DaysOfWeek.Wednesday) days.Add("WE");
                if ((daysOfWeek & DaysOfWeek.Thursday) == DaysOfWeek.Thursday) days.Add("TH");
                if ((daysOfWeek & DaysOfWeek.Friday) == DaysOfWeek.Friday) days.Add("FR");
                if ((daysOfWeek & DaysOfWeek.Saturday) == DaysOfWeek.Saturday) days.Add("SA");
                if ((daysOfWeek & DaysOfWeek.Sunday) == DaysOfWeek.Sunday) days.Add("SU");

                if (days.Count > 0)
                {
                    rule = rule.TrimEnd(';');
                    rule += $";BYDAY={string.Join(",", days)}";
                }
            }

            return rule;
        }

        /// <summary>
        /// Creates a monthly recurrence rule
        /// </summary>
        /// <param name="dayOfMonth">The day of the month (1-31)</param>
        /// <param name="interval">The interval between occurrences (e.g., every 2 months)</param>
        /// <param name="count">The number of occurrences (null for indefinite)</param>
        /// <param name="until">The end date (null for indefinite)</param>
        /// <returns>The recurrence rule</returns>
        public static string CreateMonthlyByDayRule(int dayOfMonth, int interval = 1, int? count = null, DateTime? until = null)
        {
            var rule = CreateRule(FrequencyType.Monthly, interval, count, until);
            rule = rule.TrimEnd(';');
            rule += $";BYMONTHDAY={dayOfMonth}";
            return rule;
        }

        /// <summary>
        /// Creates a monthly recurrence rule by position (e.g., first Monday)
        /// </summary>
        /// <param name="position">The position (1-5, or -1 for last)</param>
        /// <param name="dayOfWeek">The day of the week</param>
        /// <param name="interval">The interval between occurrences (e.g., every 2 months)</param>
        /// <param name="count">The number of occurrences (null for indefinite)</param>
        /// <param name="until">The end date (null for indefinite)</param>
        /// <returns>The recurrence rule</returns>
        public static string CreateMonthlyByPositionRule(int position, DaysOfWeek dayOfWeek, int interval = 1, int? count = null, DateTime? until = null)
        {
            if (position < -1 || position == 0 || position > 5)
            {
                throw new ArgumentOutOfRangeException(nameof(position), "Position must be between 1 and 5, or -1 for last");
            }

            if (dayOfWeek == DaysOfWeek.None || dayOfWeek.HasFlag(DaysOfWeek.All))
            {
                throw new ArgumentException("Only one day of the week can be specified", nameof(dayOfWeek));
            }

            var rule = CreateRule(FrequencyType.Monthly, interval, count, until);
            rule = rule.TrimEnd(';');

            string dayCode;
            if (dayOfWeek.HasFlag(DaysOfWeek.Monday)) dayCode = "MO";
            else if (dayOfWeek.HasFlag(DaysOfWeek.Tuesday)) dayCode = "TU";
            else if (dayOfWeek.HasFlag(DaysOfWeek.Wednesday)) dayCode = "WE";
            else if (dayOfWeek.HasFlag(DaysOfWeek.Thursday)) dayCode = "TH";
            else if (dayOfWeek.HasFlag(DaysOfWeek.Friday)) dayCode = "FR";
            else if (dayOfWeek.HasFlag(DaysOfWeek.Saturday)) dayCode = "SA";
            else dayCode = "SU";

            rule += $";BYDAY={position}{dayCode}";
            return rule;
        }

        /// <summary>
        /// Creates a yearly recurrence rule
        /// </summary>
        /// <param name="month">The month (1-12)</param>
        /// <param name="day">The day of the month (1-31)</param>
        /// <param name="interval">The interval between occurrences (e.g., every 2 years)</param>
        /// <param name="count">The number of occurrences (null for indefinite)</param>
        /// <param name="until">The end date (null for indefinite)</param>
        /// <returns>The recurrence rule</returns>
        public static string CreateYearlyRule(int month, int day, int interval = 1, int? count = null, DateTime? until = null)
        {
            var rule = CreateRule(FrequencyType.Yearly, interval, count, until);
            rule = rule.TrimEnd(';');
            rule += $";BYMONTH={month};BYMONTHDAY={day}";
            return rule;
        }

        /// <summary>
        /// Creates a basic recurrence rule
        /// </summary>
        /// <param name="frequency">The frequency type</param>
        /// <param name="interval">The interval between occurrences</param>
        /// <param name="count">The number of occurrences (null for indefinite)</param>
        /// <param name="until">The end date (null for indefinite)</param>
        /// <returns>The recurrence rule</returns>
        private static string CreateRule(FrequencyType frequency, int interval, int? count, DateTime? until)
        {
            var rule = $"RRULE:FREQ={frequency.ToString().ToUpper()}";

            if (interval > 1)
            {
                rule += $";INTERVAL={interval}";
            }

            if (count.HasValue)
            {
                rule += $";COUNT={count.Value}";
            }
            else if (until.HasValue)
            {
                rule += $";UNTIL={until.Value.ToUniversalTime():yyyyMMddTHHmmssZ}";
            }

            return rule;
        }
    }
}
