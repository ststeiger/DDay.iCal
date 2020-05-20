// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;


// https://raw.githubusercontent.com/dotnet/runtime/master/src/libraries/System.Private.CoreLib/src/System/TimeZoneInfo.TransitionTime.cs
namespace System
{


    // https://raw.githubusercontent.com/dotnet/runtime/master/src/libraries/System.Private.CoreLib/src/System/DateTime.cs
    public static class DateTimeInternalFunctionsExposureExtension
    {
        // Number of 100ns ticks per time unit
        private const long TicksPerMillisecond = 10000;
        private const long TicksPerSecond = TicksPerMillisecond * 1000;
        private const long TicksPerMinute = TicksPerSecond * 60;
        private const long TicksPerHour = TicksPerMinute * 60;
        private const long TicksPerDay = TicksPerHour * 24;


        // Number of days in a non-leap year
        private const int DaysPerYear = 365;
        // Number of days in 4 years
        private const int DaysPer4Years = DaysPerYear * 4 + 1;       // 1461
        // Number of days in 100 years
        private const int DaysPer100Years = DaysPer4Years * 25 - 1;  // 36524
        // Number of days in 400 years
        private const int DaysPer400Years = DaysPer100Years * 4 + 1; // 146097

        private static readonly int[] s_daysToMonth365 = {
            0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365 };
        private static readonly int[] s_daysToMonth366 = {
            0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335, 366 };


        // Exactly the same as GetDatePart, except computing all of
        // year/month/day rather than just one of them. Used when all three
        // are needed rather than redoing the computations for each.
        internal static void GetDate(this DateTime t, out int year, out int month, out int day)
        {
            
            long ticks = t.Ticks;
            // n = number of days since 1/1/0001
            int n = (int)(ticks / TicksPerDay);
            // y400 = number of whole 400-year periods since 1/1/0001
            int y400 = n / DaysPer400Years;
            // n = day number within 400-year period
            n -= y400 * DaysPer400Years;
            // y100 = number of whole 100-year periods within 400-year period
            int y100 = n / DaysPer100Years;
            // Last 100-year period has an extra day, so decrement result if 4
            if (y100 == 4) y100 = 3;
            // n = day number within 100-year period
            n -= y100 * DaysPer100Years;
            // y4 = number of whole 4-year periods within 100-year period
            int y4 = n / DaysPer4Years;
            // n = day number within 4-year period
            n -= y4 * DaysPer4Years;
            // y1 = number of whole years within 4-year period
            int y1 = n / DaysPerYear;
            // Last year has an extra day, so decrement result if 4
            if (y1 == 4) y1 = 3;
            // compute year
            year = y400 * 400 + y100 * 100 + y4 * 4 + y1 + 1;
            // n = day number within year
            n -= y1 * DaysPerYear;
            // dayOfYear = n + 1;
            // Leap year calculation looks different from IsLeapYear since y1, y4,
            // and y100 are relative to year 1, not year 0
            bool leapYear = y1 == 3 && (y4 != 24 || y100 == 3);
            int[] days = leapYear ? s_daysToMonth366 : s_daysToMonth365;
            // All months have less than 32 days, so n >> 5 is a good conservative
            // estimate for the month
            int m = (n >> 5) + 1;
            // m = 1-based month number
            while (n >= days[m]) m++;
            // compute month and day
            month = m;
            day = n - days[m - 1] + 1;
        }


    }

    public sealed partial class TimeZoneInfo
    {


        [Serializable]
        public readonly struct TransitionTime : IEquatable<TransitionTime>, ISerializable, IDeserializationCallback
        {
            private readonly DateTime _timeOfDay;
            private readonly byte _month;
            private readonly byte _week;
            private readonly byte _day;
            private readonly DayOfWeek _dayOfWeek;
            private readonly bool _isFixedDateRule;

            public DateTime TimeOfDay => _timeOfDay;

            public int Month => _month;

            public int Week => _week;

            public int Day => _day;

            public DayOfWeek DayOfWeek => _dayOfWeek;

            public bool IsFixedDateRule => _isFixedDateRule;

            public override bool Equals(object obj) =>
                obj is TransitionTime && Equals((TransitionTime)obj);

            public static bool operator ==(TransitionTime t1, TransitionTime t2) => t1.Equals(t2);

            public static bool operator !=(TransitionTime t1, TransitionTime t2) => !t1.Equals(t2);

            public bool Equals(TransitionTime other) =>
                _isFixedDateRule == other._isFixedDateRule &&
                _timeOfDay == other._timeOfDay &&
                _month == other._month &&
                (other._isFixedDateRule ?
                    _day == other._day :
                    _week == other._week && _dayOfWeek == other._dayOfWeek);

            public override int GetHashCode() => (int)_month ^ (int)_week << 8;

            private TransitionTime(DateTime timeOfDay, int month, int week, int day, DayOfWeek dayOfWeek, bool isFixedDateRule)
            {
                ValidateTransitionTime(timeOfDay, month, week, day, dayOfWeek);

                _timeOfDay = timeOfDay;
                _month = (byte)month;
                _week = (byte)week;
                _day = (byte)day;
                _dayOfWeek = dayOfWeek;
                _isFixedDateRule = isFixedDateRule;
            }

            public static TransitionTime CreateFixedDateRule(DateTime timeOfDay, int month, int day) =>
                new TransitionTime(timeOfDay, month, 1, day, DayOfWeek.Sunday, isFixedDateRule: true);

            public static TransitionTime CreateFloatingDateRule(DateTime timeOfDay, int month, int week, DayOfWeek dayOfWeek) =>
                new TransitionTime(timeOfDay, month, week, 1, dayOfWeek, isFixedDateRule: false);

            /// <summary>
            /// Helper function that validates a TransitionTime instance.
            /// </summary>
            private static void ValidateTransitionTime(DateTime timeOfDay, int month, int week, int day, DayOfWeek dayOfWeek)
            {
                if (timeOfDay.Kind != DateTimeKind.Unspecified)
                {
                    throw new ArgumentException("SR.Argument_DateTimeKindMustBeUnspecified", "timeOfDay");
                }

                // Month range 1-12
                if (month < 1 || month > 12)
                {
                    throw new ArgumentOutOfRangeException(nameof(month), "SR.ArgumentOutOfRange_MonthParam");
                }

                // Day range 1-31
                if (day < 1 || day > 31)
                {
                    throw new ArgumentOutOfRangeException(nameof(day), "SR.ArgumentOutOfRange_DayParam");
                }

                // Week range 1-5
                if (week < 1 || week > 5)
                {
                    throw new ArgumentOutOfRangeException(nameof(week), "SR.ArgumentOutOfRange_Week");
                }

                // DayOfWeek range 0-6
                if ((int)dayOfWeek < 0 || (int)dayOfWeek > 6)
                {
                    throw new ArgumentOutOfRangeException(nameof(dayOfWeek), "SR.ArgumentOutOfRange_DayOfWeek");
                }

                timeOfDay.GetDate(out int timeOfDayYear, out int timeOfDayMonth, out int timeOfDayDay);
                if (timeOfDayYear != 1 || timeOfDayMonth != 1 || timeOfDayDay != 1 || (timeOfDay.Ticks % TimeSpan.TicksPerMillisecond != 0))
                {
                    throw new ArgumentException("SR.Argument_DateTimeHasTicks", "timeOfDay");
                }
            }

            void IDeserializationCallback.OnDeserialization(object sender)
            {
                // OnDeserialization is called after each instance of this class is deserialized.
                // This callback method performs TransitionTime validation after being deserialized.

                // On Unix, TimeZoneInfos are created with empty/default TransitionTimes, since
                // TransitionTimes are not used on Unix. When deserializing TransitionTimes, only
                // validate it if it isn't an empty/default instance.
                if (this != default)
                {
                    try
                    {
                        ValidateTransitionTime(_timeOfDay, _month, _week, _day, _dayOfWeek);
                    }
                    catch (ArgumentException e)
                    {
                        throw new SerializationException("SR.Serialization_InvalidData", e);
                    }
                }
            }

            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
                if (info == null)
                {
                    throw new ArgumentNullException(nameof(info));
                }

                info.AddValue("TimeOfDay", _timeOfDay); // Do not rename (binary serialization)
                info.AddValue("Month", _month); // Do not rename (binary serialization)
                info.AddValue("Week", _week); // Do not rename (binary serialization)
                info.AddValue("Day", _day); // Do not rename (binary serialization)
                info.AddValue("DayOfWeek", _dayOfWeek); // Do not rename (binary serialization)
                info.AddValue("IsFixedDateRule", _isFixedDateRule); // Do not rename (binary serialization)
            }

            private TransitionTime(SerializationInfo info, StreamingContext context)
            {
                if (info == null)
                {
                    throw new ArgumentNullException(nameof(info));
                }

                _timeOfDay = (DateTime)info.GetValue("TimeOfDay", typeof(DateTime)); // Do not rename (binary serialization)
                _month = (byte)info.GetValue("Month", typeof(byte)); // Do not rename (binary serialization)
                _week = (byte)info.GetValue("Week", typeof(byte)); // Do not rename (binary serialization)
                _day = (byte)info.GetValue("Day", typeof(byte)); // Do not rename (binary serialization)
                _dayOfWeek = (DayOfWeek)info.GetValue("DayOfWeek", typeof(DayOfWeek)); // Do not rename (binary serialization)
                _isFixedDateRule = (bool)info.GetValue("IsFixedDateRule", typeof(bool)); // Do not rename (binary serialization)
            }
        }
    }
}
