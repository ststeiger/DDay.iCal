
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;


using Microsoft.VisualBasic.CompilerServices;
using Microsoft.Win32;

// https://www.codeproject.com/Articles/20460/Time-Zone-Class
// https://raw.githubusercontent.com/mono/mono/master/mcs/class/corlib/System/TimeZoneInfo.cs
// https://github.com/mono/mono/blob/master/mcs/class/corlib/System/TimeZoneInfo.WinRT.cs
namespace System
{

    /// <summary>
    /// Represents a time zone and provides access to all system time zones.
    /// </summary>
    [DebuggerDisplay("{_displayName}")]
    public partial class TimeZoneInfo
        : IComparer<TimeZoneInfo>
    {

        TimeSpan baseUtcOffset;
        public TimeSpan BaseUtcOffset
        {
            get { return baseUtcOffset; }
        }

        bool supportsDaylightSavingTime;
        public bool SupportsDaylightSavingTime
        {
            get { return supportsDaylightSavingTime; }
        }


        static AdjustmentRule[] ValidateRules(List<AdjustmentRule> adjustmentRules)
        {
            if (adjustmentRules == null || adjustmentRules.Count == 0)
                return null;

            AdjustmentRule prev = null;
            foreach (AdjustmentRule current in adjustmentRules.ToArray())
            {
                if (prev != null && prev.DateEnd > current.DateStart)
                {
                    adjustmentRules.Remove(current);
                }
                prev = current;
            }
            return adjustmentRules.ToArray();
        }



        private AdjustmentRule[] adjustmentRules;

        public AdjustmentRule[] GetAdjustmentRules()
        {
            if (!supportsDaylightSavingTime || adjustmentRules == null)
                return new AdjustmentRule[0];
            else
                return (AdjustmentRule[])adjustmentRules.Clone();
        }




        static TimeZoneInfo local;
        public static TimeZoneInfo Local
        {
            get
            {
                var l = local;
                if (l == null)
                {
                    l = CreateLocal();
                    if (l == null)
                        throw new System.Exception("TimeZoneNotFoundException");

                    if (Threading.Interlocked.CompareExchange(ref local, l, null) != null)
                        l = local;
                }

                return l;
            }
        }



        static RegistryKey localZoneKey;
        static RegistryKey LocalZoneKey
        {
            get
            {
                if (localZoneKey != null)
                    return localZoneKey;

                if (!IsWindows)
                    return null;

                try
                {
                    return localZoneKey = Registry.LocalMachine.OpenSubKey(
                        "SYSTEM\\CurrentControlSet\\Control\\TimeZoneInformation", false);
                }
                catch
                {
                    return null;
                }
            }
        }


        static RegistryKey timeZoneKey;
        static RegistryKey TimeZoneKey
        {
            get
            {
                if (timeZoneKey != null)
                    return timeZoneKey;
                if (!IsWindows)
                    return null;

                try
                {
                    return timeZoneKey = Registry.LocalMachine.OpenSubKey(
                        "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Time Zones",
                        false);
                }
                catch
                {
                    return null;
                }
            }
        }


        /// <summary>
        /// Needed to trim misc garbage in MS registry keys
        /// </summary>
        private static string TrimSpecial(string str)
        {
            if (str == null)
                return str;
            var Istart = 0;
            while (Istart < str.Length && !char.IsLetterOrDigit(str[Istart])) Istart++;
            var Iend = str.Length - 1;
            while (Iend > Istart && !char.IsLetterOrDigit(str[Iend]) && str[Iend] != ')') // zone name can include parentheses like "Central Standard Time (Mexico)"
                Iend--;

            return str.Substring(Istart, Iend - Istart + 1);
        }


        /// <summary>
        /// Determine whether windows of not (taken Stephane Delcroix's code)
        /// </summary>
        private static bool IsWindows
        {
            get
            {
                int platform = (int)Environment.OSVersion.Platform;
                return ((platform != 4) && (platform != 6) && (platform != 128));
            }
        }

        private static void ParseRegTzi(List<AdjustmentRule> adjustmentRules, int start_year, int end_year, byte[] buffer)
        {
            //int standard_bias = BitConverter.ToInt32 (buffer, 4); /* not sure how to handle this */
            int daylight_bias = BitConverter.ToInt32(buffer, 8);

            int standard_year = BitConverter.ToInt16(buffer, 12);
            int standard_month = BitConverter.ToInt16(buffer, 14);
            int standard_dayofweek = BitConverter.ToInt16(buffer, 16);
            int standard_day = BitConverter.ToInt16(buffer, 18);
            int standard_hour = BitConverter.ToInt16(buffer, 20);
            int standard_minute = BitConverter.ToInt16(buffer, 22);
            int standard_second = BitConverter.ToInt16(buffer, 24);
            int standard_millisecond = BitConverter.ToInt16(buffer, 26);

            int daylight_year = BitConverter.ToInt16(buffer, 28);
            int daylight_month = BitConverter.ToInt16(buffer, 30);
            int daylight_dayofweek = BitConverter.ToInt16(buffer, 32);
            int daylight_day = BitConverter.ToInt16(buffer, 34);
            int daylight_hour = BitConverter.ToInt16(buffer, 36);
            int daylight_minute = BitConverter.ToInt16(buffer, 38);
            int daylight_second = BitConverter.ToInt16(buffer, 40);
            int daylight_millisecond = BitConverter.ToInt16(buffer, 42);

            if (standard_month == 0 || daylight_month == 0)
                return;

            DateTime start_date;
            DateTime start_timeofday = new DateTime(1, 1, 1, daylight_hour, daylight_minute, daylight_second, daylight_millisecond);
            TransitionTime start_transition_time;

            start_date = new DateTime(start_year, 1, 1);
            if (daylight_year == 0)
            {
                start_transition_time = TransitionTime.CreateFloatingDateRule(
                    start_timeofday, daylight_month, daylight_day,
                    (DayOfWeek)daylight_dayofweek);
            }
            else
            {
                start_transition_time = TransitionTime.CreateFixedDateRule(
                    start_timeofday, daylight_month, daylight_day);
            }

            DateTime end_date;
            DateTime end_timeofday = new DateTime(1, 1, 1, standard_hour, standard_minute, standard_second, standard_millisecond);
            TransitionTime end_transition_time;

            end_date = new DateTime(end_year, 12, 31);
            if (standard_year == 0)
            {
                end_transition_time = TransitionTime.CreateFloatingDateRule(
                    end_timeofday, standard_month, standard_day,
                    (DayOfWeek)standard_dayofweek);
            }
            else
            {
                end_transition_time = TransitionTime.CreateFixedDateRule(
                    end_timeofday, standard_month, standard_day);
            }

            TimeSpan daylight_delta = new TimeSpan(0, -daylight_bias, 0);

            adjustmentRules.Add(AdjustmentRule.CreateAdjustmentRule(
                start_date, end_date, daylight_delta,
                start_transition_time, end_transition_time));
        }

        public static TimeZoneInfo CreateCustomTimeZone(string id, TimeSpan baseUtcOffset, string displayName, string standardDisplayName)
        {
            return CreateCustomTimeZone(id, baseUtcOffset, displayName, standardDisplayName, null, null, true);
        }

        public static TimeZoneInfo CreateCustomTimeZone(string id, TimeSpan baseUtcOffset, string displayName, string standardDisplayName, string daylightDisplayName, TimeZoneInfo.AdjustmentRule[] adjustmentRules)
        {
            return CreateCustomTimeZone(id, baseUtcOffset, displayName, standardDisplayName, daylightDisplayName, adjustmentRules, false);
        }

        public static TimeZoneInfo CreateCustomTimeZone(string id, TimeSpan baseUtcOffset, string displayName, string standardDisplayName, string daylightDisplayName, TimeZoneInfo.AdjustmentRule[] adjustmentRules, bool disableDaylightSavingTime)
        {
            if (id == null)
                throw new ArgumentNullException("id");

            if (id == String.Empty)
                throw new ArgumentException("id parameter is an empty string");

            if (baseUtcOffset.Ticks % TimeSpan.TicksPerMinute != 0)
                throw new ArgumentException("baseUtcOffset parameter does not represent a whole number of minutes");

            if (baseUtcOffset > new TimeSpan(14, 0, 0) || baseUtcOffset < new TimeSpan(-14, 0, 0))
                throw new ArgumentOutOfRangeException("baseUtcOffset parameter is greater than 14 hours or less than -14 hours");


            // return new TimeZoneInfo(id, baseUtcOffset, displayName, standardDisplayName, daylightDisplayName, adjustmentRules, disableDaylightSavingTime);
            TimeZoneInfo tfi = new TimeZoneInfo(standardDisplayName);
            tfi.baseUtcOffset = baseUtcOffset;


            tfi.supportsDaylightSavingTime = !disableDaylightSavingTime;

            if (adjustmentRules != null && adjustmentRules.Length != 0)
            {
                AdjustmentRule prev = null;
                foreach (AdjustmentRule current in adjustmentRules)
                {
                    if (current == null)
                        throw new System.Exception("InvalidTimeZoneException - one or more elements in adjustmentRules are null");

                    if ((baseUtcOffset + current.DaylightDelta < new TimeSpan(-14, 0, 0)) ||
                            (baseUtcOffset + current.DaylightDelta > new TimeSpan(14, 0, 0)))
                        throw new System.Exception("InvalidTimeZoneException - Sum of baseUtcOffset and DaylightDelta of one or more object in adjustmentRules array is greater than 14 or less than -14 hours;");

                    if (prev != null && prev.DateStart > current.DateStart)
                        throw new System.Exception("InvalidTimeZoneException - adjustment rules specified in adjustmentRules parameter are not in chronological order");

                    if (prev != null && prev.DateEnd > current.DateStart)
                        throw new System.Exception("InvalidTimeZoneException - some adjustment rules in the adjustmentRules parameter overlap");

                    if (prev != null && prev.DateEnd == current.DateStart)
                        throw new System.Exception("InvalidTimeZoneException - a date can have multiple adjustment rules applied to it");

                    prev = current;
                }
            }
            else
            {
                tfi.supportsDaylightSavingTime = false;
            }

            tfi.adjustmentRules = adjustmentRules;

            return tfi;
        }



        private static TimeZoneInfo FromRegistryKey(string id, RegistryKey key)
        {
            byte[] reg_tzi = (byte[])key.GetValue("TZI");

            if (reg_tzi == null)
                throw new System.Exception("InvalidTimeZoneException");

            int bias = BitConverter.ToInt32(reg_tzi, 0);
            TimeSpan baseUtcOffset = new TimeSpan(0, -bias, 0);

            string display_name = (string)key.GetValue("Display");
            string standard_name = (string)key.GetValue("Std");
            string daylight_name = (string)key.GetValue("Dlt");

            List<AdjustmentRule> adjustmentRules = new List<AdjustmentRule>();

            RegistryKey dst_key = key.OpenSubKey("Dynamic DST", false);
            if (dst_key != null)
            {
                int first_year = (int)dst_key.GetValue("FirstEntry");
                int last_year = (int)dst_key.GetValue("LastEntry");
                int year;

                for (year = first_year; year <= last_year; year++)
                {
                    byte[] dst_tzi = (byte[])dst_key.GetValue(year.ToString());
                    if (dst_tzi != null)
                    {
                        int start_year = year == first_year ? 1 : year;
                        int end_year = year == last_year ? 9999 : year;
                        ParseRegTzi(adjustmentRules, start_year, end_year, dst_tzi);
                    }
                }
            }
            else
                ParseRegTzi(adjustmentRules, 1, 9999, reg_tzi);

            return CreateCustomTimeZone(id, baseUtcOffset, display_name, standard_name, daylight_name, ValidateRules(adjustmentRules));
        }



        public static TimeZoneInfo FindSystemTimeZoneById(string id)
        {
            //FIXME: this method should check for cached values in systemTimeZones
            if (id == null)
                throw new ArgumentNullException("id");

#if true // WIN_PLATFORM
            if (TimeZoneKey != null)
            {
                if (id == "Coordinated Universal Time")
                    id = "UTC"; //windows xp exception for "StandardName" property
                RegistryKey key = TimeZoneKey.OpenSubKey(id, false);
                if (key == null)
                    throw new System.Exception("TimeZoneNotFoundException");
                return FromRegistryKey(id, key);
            }
            else if (IsWindows)
            {
                return null; // FindSystemTimeZoneByIdWinRTFallback (id);
            }
#endif
            // Local requires special logic that already exists in the Local property (bug #326)
            if (id == "Local")
                return Local;

            return null; // FindSystemTimeZoneByIdCore(id);
        }

        static TimeZoneInfo CreateLocal()
        {
#if true // WIN_PLATFORM
            if (IsWindows && LocalZoneKey != null)
            {
                string name = (string)LocalZoneKey.GetValue("TimeZoneKeyName");
                if (name == null)
                    name = (string)LocalZoneKey.GetValue("StandardName"); // windows xp
                name = TrimSpecial(name);
                if (name != null)
                    return TimeZoneInfo.FindSystemTimeZoneById(name);
            }
            // else if (IsWindows) { return GetLocalTimeZoneInfoWinRTFallback(); }
#endif
            return null;
        }


        private struct SYSTEMTIME
        {
            public ushort wYear;

            public ushort wMonth;

            public ushort wDayOfWeek;

            public ushort wDay;

            public ushort wHour;

            public ushort wMinute;

            public ushort wSecond;

            public ushort wMilliseconds;

            /// <summary>
            /// Sets the member values of the time structure.
            /// </summary>
            /// <param name="info">A byte array that contains the information of a time.</param>
            [DebuggerHidden]
            public void SetInfo(byte[] info)
            {
                if (info.Length != Marshal.SizeOf(this))
                {
                    throw new ArgumentException("Information size is incorrect", "info");
                }
                wYear = BitConverter.ToUInt16(info, 0);
                wMonth = BitConverter.ToUInt16(info, 2);
                wDayOfWeek = BitConverter.ToUInt16(info, 4);
                wDay = BitConverter.ToUInt16(info, 6);
                wHour = BitConverter.ToUInt16(info, 8);
                wMinute = BitConverter.ToUInt16(info, 10);
                wSecond = BitConverter.ToUInt16(info, 12);
                wMilliseconds = BitConverter.ToUInt16(info, 14);
            }

            /// <summary>
            /// Determines whether the specified System.Object 
            /// is equal to the current System.Object.
            /// </summary>
            /// <param name="obj">The System.Object to compare 
            /// with the current System.Object.</param>
            [DebuggerHidden]
            public override bool Equals(object obj)
            {
                if (GetType() == obj.GetType())
                {
                    SYSTEMTIME objSt = (SYSTEMTIME)obj;
                    if (wDay != objSt.wDay || wDayOfWeek != objSt.wDayOfWeek || wHour != objSt.wHour || wMilliseconds != objSt.wMilliseconds || wMinute != objSt.wMinute || wMonth != objSt.wMonth || wSecond != objSt.wSecond || wYear != objSt.wYear)
                    {
                        return false;
                    }
                    return true;
                }
                return false;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct TimeZoneInformation
        {
            public int bias;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string standardName;

            public SYSTEMTIME standardDate;

            public int standardBias;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string daylightName;

            public SYSTEMTIME daylightDate;

            public int daylightBias;

            /// <summary>
            /// Sets the member values of bias, standardBias, 
            /// daylightBias, standardDate, daylightDate of the structure.
            /// </summary>
            /// <param name="info">A byte array that contains the 
            /// information of the Tzi windows registry key.</param>
            [DebuggerHidden]
            public void SetBytes(byte[] info)
            {
                if (info.Length != 44)
                {
                    throw new ArgumentException("Information size is incorrect", "info");
                }
                bias = BitConverter.ToInt32(info, 0);
                standardBias = BitConverter.ToInt32(info, 4);
                daylightBias = BitConverter.ToInt32(info, 8);
                byte[] helper = new byte[16];
                Array.Copy(info, 12, helper, 0, 16);
                standardDate.SetInfo(helper);
                Array.Copy(info, 28, helper, 0, 16);
                daylightDate.SetInfo(helper);
            }

            /// <summary>
            /// Determines whether the specified System.Object 
            /// is equal to the current System.Object.
            /// </summary>
            /// <param name="obj">The System.Object to compare 
            /// with the current System.Object.</param>
            [DebuggerHidden]
            public override bool Equals(object obj)
            {
                if (GetType() == obj.GetType())
                {
                    TimeZoneInformation objTzi = (TimeZoneInformation)obj;
                    if (bias != objTzi.bias || daylightBias != objTzi.daylightBias || Operators.CompareString(daylightName, objTzi.daylightName, false) != 0 || standardBias != objTzi.standardBias || Operators.CompareString(standardName, objTzi.standardName, false) != 0 || !daylightDate.Equals(objTzi.daylightDate) || !standardDate.Equals(objTzi.standardDate))
                    {
                        return false;
                    }
                    return true;
                }
                return false;
            }
        }

        private string _id;

        private TimeZoneInformation _tzi;

        private string _displayName;

        /// <summary>
        /// Gets the display name of the time zone.
        /// </summary>
        public string DisplayName
        {
            [DebuggerHidden]
            get
            {
                Refresh();
                return _displayName;
            }
        }

        /// <summary>
        /// Gets the daylight saving name of the time zone.
        /// </summary>
        public string DaylightName
        {
            [DebuggerHidden]
            get
            {
                Refresh();
                if (GetDaylightChanges(CurrentTime.Year).Delta == TimeSpan.Zero)
                {
                    return _tzi.standardName;
                }
                return _tzi.daylightName;
            }
        }

        /// <summary>
        /// Gets the standard name of the time zone.
        /// </summary>
        public string StandardName
        {
            [DebuggerHidden]
            get
            {
                Refresh();
                return _tzi.standardName;
            }
        }

        /// <summary>
        /// Gets the current date and time of the time zone.
        /// </summary>
        public DateTime CurrentTime
        {
            [DebuggerHidden]
            get
            {
                DateTime CurrentTime = new DateTime(checked(DateTime.UtcNow.Ticks + CurrentUtcOffset.Ticks), DateTimeKind.Local);
                return CurrentTime;
            }
        }

        /// <summary>
        /// Gets the current UTC (Coordinated Universal Time) offset of the time zone.
        /// </summary>
        public TimeSpan CurrentUtcOffset
        {
            [DebuggerHidden]
            get
            {
                return checked(IsDaylightSavingTime() ? new TimeSpan(0, -(_tzi.bias + _tzi.daylightBias), 0) : new TimeSpan(0, -_tzi.bias, 0));
            }
        }

        /// <summary>
        /// Gets or sets the current time zone for this computer system.
        /// </summary>
        public static TimeZoneInfo CurrentTimeZone
        {
            [DebuggerHidden]
            get
            {
                return new TimeZoneInfo(TimeZone.CurrentTimeZone.StandardName);
            }
            [DebuggerHidden]
            set
            {
                value.Refresh();
                if (!SetTimeZoneInformation(ref value._tzi))
                {
                    throw new Win32Exception();
                }
            }
        }

        /// <summary>
        /// Gets the standard UTC (Coordinated Universal Time) offset of the time zone.
        /// </summary>
        public TimeSpan StandardUtcOffset
        {
            [DebuggerHidden]
            get
            {
                Refresh();
                return new TimeSpan(0, checked(-_tzi.bias), 0);
            }
        }

        /// <summary>
        /// Gets the id of the time zone.
        /// </summary>
        public string Id
        {
            [DebuggerHidden]
            get
            {
                Refresh();
                return _id;
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetTimeZoneInformation(ref TimeZoneInformation lpTimeZoneInformation);

        /// <param name="standardName">A time zone standard name.</param>
        [DebuggerHidden]
        public TimeZoneInfo(string standardName)
        {
            _tzi = default(TimeZoneInformation);
            SetValues(standardName);
        }

        [DebuggerHidden]
        private TimeZoneInfo()
        {
            _tzi = default(TimeZoneInformation);
        }

        /// <summary>
        /// Gets an array of all time zones on the system.
        /// </summary>
        [DebuggerHidden]
        public static TimeZoneInfo[] GetTimeZones()
        {
            List<TimeZoneInfo> tzInfos = new List<TimeZoneInfo>();
            RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Time Zones", false);
            if (key != null)
            {
                string[] subKeyNames = key.GetSubKeyNames();
                foreach (string zoneName in subKeyNames)
                {
                    TimeZoneInfo tzi = new TimeZoneInfo();
                    tzi._id = zoneName;
                    tzi.SetValues();
                    tzInfos.Add(tzi);
                }
                Sort(tzInfos);
                return tzInfos.ToArray();
            }
            throw new KeyNotFoundException("Cannot find the windows registry key (Time Zone).");
        }

        /// <summary>
        /// Sorts the elements in a list(Of TimeZoneInfo) 
        /// object based on standard UTC offset or display name.
        /// </summary>
        /// <param name="tzInfos">A time zone list to sort.</param>
        [DebuggerHidden]
        public static void Sort(List<TimeZoneInfo> tzInfos)
        {
            tzInfos.Sort(new TimeZoneInfo());
        }

        /// <summary>
        /// Sorts the elements in an entire one-dimensional TimeZoneInfo 
        /// array based on standard UTC offset or display name.
        /// </summary>
        /// <param name="tzInfos">A time zone array to sort.</param>
        [DebuggerHidden]
        public static void Sort(TimeZoneInfo[] tzInfos)
        {
            Array.Sort(tzInfos, new TimeZoneInfo());
        }

        /// <summary>
        /// Gets a TimeZoneInfo.Object from standard name.
        /// </summary>
        /// <param name="standardName">A time zone standard name.</param>
        [DebuggerHidden]
        public static TimeZoneInfo FromStandardName(string standardName)
        {
            return new TimeZoneInfo(standardName);
        }

        /// <summary>
        /// Gets a TimeZoneInfo.Object from Id.
        /// </summary>
        /// <param name="id">A time zone id that corresponds 
        /// to the windows registry time zone key.</param>
        [DebuggerHidden]
        public static TimeZoneInfo FromId(string id)
        {
            if (id != null)
            {
                if (Operators.CompareString(id, string.Empty, false) != 0)
                {
                    RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Time Zones", false);
                    if (key == null)
                    {
                        throw new KeyNotFoundException("Cannot find the windows registry key (Time Zone).");
                    }
                    RegistryKey subKey = key.OpenSubKey(id, false);
                    if (subKey != null)
                    {
                        TimeZoneInfo tzi = new TimeZoneInfo();
                        tzi._id = subKey.Name;
                        tzi._displayName = Conversions.ToString(subKey.GetValue("Display"));
                        tzi._tzi.daylightName = Conversions.ToString(subKey.GetValue("Dlt"));
                        tzi._tzi.standardName = Conversions.ToString(subKey.GetValue("Std"));
                        tzi._tzi.SetBytes((byte[])subKey.GetValue("Tzi"));
                        return tzi;
                    }
                }
                throw new ArgumentException("Unknown time zone.", "id");
            }
            throw new ArgumentNullException("id", "Value cannot be null.");
        }

        /// <summary>
        /// Returns the daylight saving time for a particular year.
        /// </summary>
        /// <param name="year">The year to which the daylight 
        /// saving time period applies.</param>
        [DebuggerHidden]
        public DaylightTime GetDaylightChanges(int year)
        {
            TimeZoneInformation tzi = default(TimeZoneInformation);
            RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Time Zones", false);
            if (key != null)
            {
                RegistryKey subKey = key.OpenSubKey(_id, false);
                if (subKey != null)
                {
                    RegistryKey subKey2 = subKey.OpenSubKey("Dynamic DST", false);
                    if (subKey2 != null)
                    {
                        if (Array.IndexOf(subKey2.GetValueNames(), Conversions.ToString(year)) != -1)
                        {
                            tzi.SetBytes((byte[])subKey2.GetValue(Conversions.ToString(year)));
                        }
                        else
                        {
                            Refresh();
                            tzi = _tzi;
                        }
                    }
                    else
                    {
                        Refresh();
                        tzi = _tzi;
                    }
                    DateTime dStart = GetStartDate(tzi, year);
                    DateTime dEnd = GetEndDate(tzi, year);
                    if (DateTime.Compare(dStart, DateTime.MinValue) != 0 && DateTime.Compare(dEnd, DateTime.MinValue) != 0)
                    {
                        return new DaylightTime(dStart, dEnd, new TimeSpan(0, checked(-_tzi.daylightBias), 0));
                    }
                    return new DaylightTime(dStart, dEnd, new TimeSpan(0, 0, 0));
                }
                throw new Exception("Unknown time zone.");
            }
            throw new KeyNotFoundException("Cannot find the windows registry key (Time Zone).");
        }

        /// <summary>
        /// Returns a value indicating whether this time 
        /// zone is within a daylight saving time period.
        /// </summary>
        [DebuggerHidden]
        public bool IsDaylightSavingTime()
        {
            DateTime dUtcNow;
            DateTime sUtcNow;
            checked
            {
                dUtcNow = DateTime.UtcNow.AddMinutes(-_tzi.bias);
                sUtcNow = DateTime.UtcNow.AddMinutes(-(_tzi.bias + _tzi.daylightBias));
            }
            if ((uint)_tzi.daylightDate.wMonth <= (uint)_tzi.standardDate.wMonth)
            {
                DaylightTime dt = GetDaylightChanges(dUtcNow.Year);
                if (dt.Delta != TimeSpan.Zero)
                {
                    if (DateTime.Compare(dUtcNow, dt.Start) >= 0 && DateTime.Compare(sUtcNow, dt.End) < 0)
                    {
                        return true;
                    }
                    return false;
                }
            }
            else
            {
                DaylightTime dt = GetDaylightChanges(sUtcNow.Year);
                if (dt.Delta != TimeSpan.Zero)
                {
                    if (DateTime.Compare(dUtcNow, dt.Start) < 0 && DateTime.Compare(sUtcNow, dt.End) >= 0)
                    {
                        return false;
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Creates and returns a date and time object.
        /// </summary>
        /// <param name="wYear">The year of the date.</param>
        /// <param name="wMonth">The month of the date.</param>
        /// <param name="wDay">The week day in the month.</param>
        /// <param name="wDayOfWeek">The day of the week.</param>
        /// <param name="wHour">The hour of the date.</param>
        /// <param name="wMinute">The minute of the date.</param>
        /// <param name="wSecond">The seconds of the date.</param>
        /// <param name="wMilliseconds">The milliseconds of the date.</param>
        [DebuggerHidden]
        private DateTime CreateDate(int wYear, int wMonth, int wDay, int wDayOfWeek, int wHour, int wMinute, int wSecond, int wMilliseconds)
        {
            if (wDay < 1 || wDay > 5)
            {
                throw new ArgumentOutOfRangeException("wDat", wDay, "The value is out of acceptable range (1 to 5).");
            }
            if (wDayOfWeek < 0 || wDayOfWeek > 6)
            {
                throw new ArgumentOutOfRangeException("wDayOfWeek", wDayOfWeek, "The value is out of acceptable range (0 to 6).");
            }
            int daysInMonth = DateTime.DaysInMonth(wYear, wMonth);
            int fDayOfWeek = (int)new DateTime(wYear, wMonth, 1).DayOfWeek;
            int occurre = 1;
            int day = 1;
            checked
            {
                if (fDayOfWeek != wDayOfWeek)
                {
                    if (wDayOfWeek == 0)
                    {
                        day += 7 - fDayOfWeek;
                    }
                    else if (wDayOfWeek > fDayOfWeek)
                    {
                        day += wDayOfWeek - fDayOfWeek;
                    }
                    else if (wDayOfWeek < fDayOfWeek)
                    {
                        day = wDayOfWeek + fDayOfWeek;
                    }
                }
                for (; occurre < wDay; occurre++)
                {
                    if (day > daysInMonth - 7)
                    {
                        break;
                    }
                    day += 7;
                }
                return new DateTime(wYear, wMonth, day, wHour, wMinute, wSecond, wMilliseconds, DateTimeKind.Local);
            }
        }

        /// <summary>
        /// Gets the starting daylight saving date and time for specified thime zone.
        /// </summary>
        [DebuggerHidden]
        private DateTime GetStartDate(TimeZoneInformation tzi, int year)
        {
            ref SYSTEMTIME daylightDate = ref tzi.daylightDate;
            DateTime GetStartDate = default(DateTime);
            if (daylightDate.wMonth != 0)
            {
                if (daylightDate.wYear == 0)
                {
                    return CreateDate(year, daylightDate.wMonth, daylightDate.wDay, daylightDate.wDayOfWeek, daylightDate.wHour, daylightDate.wMinute, daylightDate.wSecond, daylightDate.wMilliseconds);
                }
                GetStartDate = new DateTime(daylightDate.wYear, daylightDate.wMonth, daylightDate.wDay, daylightDate.wHour, daylightDate.wMinute, daylightDate.wSecond, daylightDate.wMilliseconds, DateTimeKind.Local);
            }
            return GetStartDate;
        }

        /// <summary>
        /// Gets the end date of the daylight saving time for specified thime zone.
        /// </summary>
        [DebuggerHidden]
        private DateTime GetEndDate(TimeZoneInformation tzi, int year)
        {
            ref SYSTEMTIME standardDate = ref tzi.standardDate;
            DateTime GetEndDate = default(DateTime);
            if (standardDate.wMonth != 0)
            {
                if (standardDate.wYear == 0)
                {
                    return CreateDate(year, standardDate.wMonth, standardDate.wDay, standardDate.wDayOfWeek, standardDate.wHour, standardDate.wMinute, standardDate.wSecond, standardDate.wMilliseconds);
                }
                GetEndDate = new DateTime(standardDate.wYear, standardDate.wMonth, standardDate.wDay, standardDate.wHour, standardDate.wMinute, standardDate.wSecond, standardDate.wMilliseconds, DateTimeKind.Local);
            }
            return GetEndDate;
        }

        /// <summary>
        /// Refreshes the information of the time zone object.
        /// </summary>
        [DebuggerHidden]
        public void Refresh()
        {
            SetValues();
        }

        /// <summary>
        /// Sets the time zone object's information.
        /// </summary>
        [DebuggerHidden]
        private void SetValues()
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Time Zones", false);
            if (key != null)
            {
                RegistryKey subKey = key.OpenSubKey(_id, false);
                if (subKey != null)
                {
                    _displayName = Conversions.ToString(subKey.GetValue("Display"));
                    _tzi.daylightName = Conversions.ToString(subKey.GetValue("Dlt"));
                    _tzi.standardName = Conversions.ToString(subKey.GetValue("Std"));
                    _tzi.SetBytes((byte[])subKey.GetValue("Tzi"));
                    return;
                }
                throw new Exception("Unknown time zone.");
            }
            throw new KeyNotFoundException("Cannot find the windows registry key (Time Zone).");
        }

        /// <summary>
        /// Sets the time zone object's information.
        /// </summary>
        /// <param name="standardName">A time zone standard name.</param>
        [DebuggerHidden]
        private void SetValues(string standardName)
        {
            if (standardName != null)
            {
                bool exist = false;
                if (Operators.CompareString(standardName, string.Empty, false) != 0)
                {
                    RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Time Zones", false);
                    if (key == null)
                    {
                        throw new KeyNotFoundException("Cannot find the windows registry key (Time Zone).");
                    }
                    string[] subKeyNames = key.GetSubKeyNames();
                    foreach (string zoneName in subKeyNames)
                    {
                        RegistryKey subKey = key.OpenSubKey(zoneName, false);
                        if (Operators.CompareString(Conversions.ToString(subKey.GetValue("Std")), standardName, false) == 0)
                        {
                            _id = zoneName;
                            _displayName = Conversions.ToString(subKey.GetValue("Display"));
                            _tzi.daylightName = Conversions.ToString(subKey.GetValue("Dlt"));
                            _tzi.standardName = Conversions.ToString(subKey.GetValue("Std"));
                            _tzi.SetBytes((byte[])subKey.GetValue("Tzi"));
                            exist = true;
                            break;
                        }
                    }
                }
                if (!exist)
                {
                    throw new ArgumentException("Unknown time zone.", "standardName");
                }
                return;
            }
            throw new ArgumentNullException("id", "Value cannot be null.");
        }

        /// <summary>
        /// Returns a System.String that represents the current TimeZoneInfo object.
        /// </summary>
        [DebuggerHidden]
        public override string ToString()
        {
            return DisplayName;
        }

        /// <summary>
        /// Determines whether the specified System.Object 
        /// is equal to the current System.Object.
        /// </summary>
        /// <param name="obj">The System.Object to compare 
        /// with the current System.Object.</param>
        [DebuggerHidden]
        public override bool Equals(object obj)
        {
            if (GetType() == obj.GetType())
            {
                TimeZoneInfo objTzi = (TimeZoneInfo)obj;
                if (Operators.CompareString(_displayName, objTzi._displayName, false) != 0 || Operators.CompareString(_id, objTzi._id, false) != 0 || !_tzi.Equals(objTzi._tzi))
                {
                    return false;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Compares two specified TimeZoneInfo.Objects 
        /// based on standard UTC offset or display name.
        /// </summary>
        /// <param name="x">The first TimeZoneInfo.Object.</param>
        /// <param name="y">The second TimeZoneInfo.Object.</param>
        [DebuggerHidden]
        protected int Compare(TimeZoneInfo x, TimeZoneInfo y)
        {
            if (x._tzi.bias == y._tzi.bias)
            {
                return x._displayName.CompareTo(y._displayName);
            }
            if (x._tzi.bias > y._tzi.bias)
            {
                return -1;
            }
            if (x._tzi.bias < y._tzi.bias)
            {
                return 1;
            }
            int Compare = default(int);
            return Compare;
        }

        int IComparer<TimeZoneInfo>.Compare(TimeZoneInfo x, TimeZoneInfo y)
        {
            //ILSpy generated this explicit interface implementation from .override directive in Compare
            return this.Compare(x, y);
        }
    }

}