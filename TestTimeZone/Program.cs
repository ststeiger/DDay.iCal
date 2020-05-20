
using DDay.iCal;
using DDay.iCal.Serialization.iCalendar;
using System;

namespace TestTimeZone
{


    static class Program
    {

        /// <summary>
        /// Creates a string representation of an event.
        /// </summary>
        /// <param name="evt">The event to display</param>
        /// <returns>A string representation of the event.</returns>
        static string GetDescription(IEvent evt)
        {
            string summary = evt.Summary + ": " + evt.Start.Local.ToShortDateString();

            if (evt.IsAllDay)
            {
                return summary + " (all day)";
            }
            else
            {
                summary += ", " + evt.Start.Local.ToShortTimeString();
                return summary + " (" + Math.Round((double)evt.End.Subtract(evt.Start).TotalHours) + " hours)";
            }
        }

        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [System.STAThread]
        static void Main()
        {
#if false
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
            System.Windows.Forms.Application.Run(new Form1());
#endif 

            iCalendar iCal = new iCalendar();

            // Create the event, and add it to the iCalendar
            Event evt = iCal.Create<Event>();
            if (false)
            {
                evt.Status = EventStatus.Confirmed;
                evt.Sequence = 0;
            }
            else
            {
                // iCal URL feeds vs iCal file!
                // https://en.wikipedia.org/wiki/ICalendar#Events_.28VEVENT.29

                // In the above, note the following:
                // 0. The UID must be the same as the original event
                //    AND the SEQUENCE: number must be the CURRENT sequence number! 
                //    (you do not need to add 1 from the last sequence number as cancelling the event does not count as an update).
                // 1. The sequence number is higher than the original event's sequence number (which was "0", in this case)
                // 2. METHOD is set to "CANCEL"
                // 3. The STATUS of the event is set to "CANCEL"
                // 4. FYI Google is case-sensitive for CANCELLED; STATUS:Cancelled fails silently.

                evt.Status = EventStatus.Cancelled;
                // Must set Method: Cancel
                iCal.Method = "CANCEL";
                evt.Sequence = 1;
            }

            


            // Set information about the event
            evt.Start = iCalDateTime.Today.AddHours(8);
            evt.End = evt.Start.AddHours(18); // This also sets the duration
            evt.Description = "The event description";
            evt.Location = "Event location";
            evt.Summary = "18 hour event summary";
            evt.UID = System.Guid.Empty.ToString();

            // evt.UID = System.Guid.Empty.ToString();
            /*
            // Set information about the second event
            evt = iCal.Create<Event>();
            evt.Start = iCalDateTime.Today.AddDays(5);
            evt.End = evt.Start.AddDays(1);
            evt.IsAllDay = true;
            evt.Summary = "All-day event";
            */


            // Display each event
            foreach (Event e in iCal.Events)
                Console.WriteLine("Event created: " + GetDescription(e));

            // Serialize (save) the iCalendar
            iCalendarSerializer serializer = new iCalendarSerializer();
            serializer.Serialize(iCal, @"iCalendar.ics");
            Console.WriteLine("iCalendar file saved." + Environment.NewLine);

            // Load the calendar from the file we just saved
            IICalendarCollection calendars = iCalendar.LoadFromFile(@"iCalendar.ics");
            Console.WriteLine("iCalendar file loaded.");

            // Iterate through each event to display its description
            // (and verify the file saved correctly)
            foreach (IICalendar calendar in calendars)
            {
                foreach (IEvent e in calendar.Events)
                    Console.WriteLine("Event loaded: " + GetDescription(e));
            }


            System.Console.WriteLine(System.TimeZoneInfo.Local);
            System.Console.WriteLine(System.TimeZoneInfo.Local.BaseUtcOffset);

            System.Console.WriteLine(System.Environment.NewLine);
            System.Console.WriteLine(" --- Press any key to continue");
            System.Console.ReadKey();
        }


    }


}
