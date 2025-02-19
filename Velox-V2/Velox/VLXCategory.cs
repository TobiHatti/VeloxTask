﻿using Syncfusion.Windows.Forms.Tools.MultiColumnTreeView;
using Syncfusion.Windows.Forms.Tools.Win32API;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WrapSQL;

namespace Velox
{
    public enum TimeSelection
    {
        // Note: Enum-Values are bound to combobox-index
        Today = 0,
        Yesterday = 1,
        ThisWeek = 2,
        LastWeek = 3,
        ThisMonth = 4,
        LastMonth = 5,
        Total = 6,

        // Not for selection!
        CustomRange = 7
    }

    public class VLXCategory
    {
        public string ID { get; } = Guid.Empty.ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<VLXTimestamp> Timestamps { get; set; } = new List<VLXTimestamp>();
        public Color CategoryColor { get; set; } = Color.Red;

        public bool SessionActive { get; private set; }
        public TimeSpan CurrentSessionTime
        {
            get
            {
                if (sessionStartTime != DateTime.MinValue) return DateTime.Now - sessionStartTime;
                else return TimeSpan.Zero;
            }
        }

        public TimeSpan TotalTime
        {
            get
            {
                TimeSpan tsTotal = TimeSpan.Zero;
                foreach (VLXTimestamp ts in Timestamps)
                    tsTotal = tsTotal.Add(ts.TimeSpan);
                return tsTotal;
            }
        }

        public VLXTimestamp HistoricalFirstTimestamp
        {
            get
            {
                VLXTimestamp minTS = new VLXTimestamp();
                minTS.StartTime = DateTime.MaxValue;

                foreach (VLXTimestamp ts in Timestamps)
                    if(ts.StartTime.Ticks < minTS.StartTime.Ticks)
                        minTS = ts;

                return minTS;
            }
        }

        public VLXTimestamp HistoricalLastTimestamp
        {
            get
            {
                VLXTimestamp maxTS = new VLXTimestamp();
                maxTS.EndTime = DateTime.MinValue;

                foreach (VLXTimestamp ts in Timestamps)
                    if (ts.EndTime.Ticks > maxTS.EndTime.Ticks)
                        maxTS = ts;

                return maxTS;
            }
        }

        private DateTime sessionStartTime = DateTime.MinValue;
        private DateTime sessionEndTime = DateTime.MinValue;
        

        public VLXCategory(string ID)
        {
            this.ID = ID;
        }

        public static VLXCategory CreateCategory(WrapSQLite sql)
        {
            string categoryID = Guid.NewGuid().ToString();
            string defaultName = "New Category";
            string defaultDescription = string.Empty;
            Color defaultColor = Color.DodgerBlue;

            try
            {
                sql.Open();
                sql.ExecuteNonQuery($"INSERT INTO {VLXDB.Category.Self} ({VLXDB.Category.ID}, {VLXDB.Category.Name}, {VLXDB.Category.Description}, {VLXDB.Category.Color}) VALUES (?,?,?,?)", categoryID.ToString(), defaultName, defaultDescription, defaultColor.ToArgb());
                sql.Close();

                return new VLXCategory(categoryID) { Name = defaultName, Description = defaultDescription };
            }
            catch
            {
                return null;
            } 
        }

        public void Delete(WrapSQLite sql)
        {
            sql.Open();
            sql.TransactionBegin();
            try
            {
                // Delete Category from DB
                sql.ExecuteNonQuery($"DELETE FROM {VLXDB.Category.Self} WHERE {VLXDB.Category.ID} = ?", ID);

                // Delete Timestamps from DB
                sql.ExecuteNonQuery($"DELETE FROM {VLXDB.Timestamps.Self} WHERE {VLXDB.Timestamps.CategoryID} = ?", ID);

                sql.TransactionCommit();
            }
            catch
            { 
                sql.TransactionRollback();
            }
            sql.Close();


            
        }

        public TimeSpan TotalTimeFromSelection(TimeSelection pSelection)
        {
            DateTime DTN = DateTime.Now;
            DateTime DTLM = DateTime.Now.AddMonths(-1);
            DateTime DTLW = DateTime.Now.AddDays(-7);

            switch (pSelection)
            {
                case TimeSelection.Today:
                    return TotalTimeFromSpan(
                        new DateTime(DTN.Year, DTN.Month, DTN.Day, 0, 0, 0), 
                        new DateTime(DTN.Year, DTN.Month, DTN.Day, 23, 59, 59)
                    );
                case TimeSelection.Yesterday:
                    return TotalTimeFromSpan(
                        new DateTime(DTN.Year, DTN.Month, DTN.Day, 0, 0, 0).AddDays(-1),
                        new DateTime(DTN.Year, DTN.Month, DTN.Day, 23, 59, 59).AddDays(-1)
                    );
                case TimeSelection.ThisWeek:
                    return TotalTimeFromIsoWeek(DTN.Year, GetIso8601WeekOfYear(DTN));
                case TimeSelection.LastWeek:
                    return TotalTimeFromIsoWeek(DTLW.Year, GetIso8601WeekOfYear(DTLW));
                case TimeSelection.ThisMonth:
                    return TotalTimeFromSpan(
                        new DateTime(DTN.Year, DTN.Month, 1, 0, 0, 0),
                        new DateTime(DTN.Year, DTN.Month, DateTime.DaysInMonth(DTN.Year, DTN.Month), 23, 59, 59)
                    );
                case TimeSelection.LastMonth:
                    return TotalTimeFromSpan(
                        new DateTime(DTLM.Year, DTLM.Month, 1, 0, 0, 0),
                        new DateTime(DTLM.Year, DTLM.Month, DateTime.DaysInMonth(DTLM.Year, DTLM.Month), 23, 59, 59)
                    );
                default:
                    return TotalTime;
            }
        }

        public TimeSpan TotalTimeFromSpan(DateTime pSelectionStart, DateTime pSelectionEnd)
        {
            TimeSpan cummulated = TimeSpan.Zero;

            // Required, otherwise timespans on the end-date will not get counted.
            pSelectionEnd = pSelectionEnd.AddDays(1);

            foreach (VLXTimestamp ts in Timestamps)
                if (ts.StartTime.Ticks > pSelectionStart.Ticks && ts.EndTime.Ticks < pSelectionEnd.Ticks) cummulated += ts.TimeSpan;
            
            return cummulated;
        }

        public TimeSpan TotalTimeFromIsoWeek(int year, int week)
        {
            DateTime firstDayOfWeek = FirstDateOfWeekISO8601(year, week);
            return TotalTimeFromSpan(firstDayOfWeek, firstDayOfWeek.AddDays(7));
        }

        public void StartSession()
        {
            SessionActive = true;
            sessionStartTime = DateTime.Now;
        }

        public void StopSession()
        {
            SessionActive = false;
            sessionEndTime = DateTime.Now;
        }

        public bool SaveLastSession(WrapSQLite sql)
        {
            bool success = false;
            bool saveEntry = true;
            if (!SessionActive)
            {
                if((sessionEndTime - sessionStartTime).TotalMinutes < 1)
                {
                    saveEntry = false;
                    if (MessageBox.Show($"The recorded time is below 1 minute ({(sessionEndTime - sessionStartTime).ToString("ss")}s). Do you still want to save this record?", "Short record", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        saveEntry = true;
                    }
                }

                if (saveEntry)
                {
                    success = SaveManualSession(sql, sessionStartTime, sessionEndTime);
                }
                else success = true;

                sessionStartTime = DateTime.MinValue;
                sessionEndTime = DateTime.MinValue;
            }

            return success;
        }

        public bool SaveManualSession(WrapSQLite sql, DateTime startTime, DateTime endTime)
        {
            bool success = true;

            string TimestampID = Guid.NewGuid().ToString();

            try
            {
                sql.Open();

                sql.ExecuteNonQuery($"INSERT INTO {VLXDB.Timestamps.Self} ({VLXDB.Timestamps.ID},{VLXDB.Timestamps.CategoryID},{VLXDB.Timestamps.StartTime},{VLXDB.Timestamps.EndTime}) VALUES (?,?,?,?)",
                    TimestampID,
                    ID,
                    startTime,
                    endTime
                );

                sql.Close();
            }
            catch (Exception ex)
            {
                VLXException.GlobalErrorReport = ex.Message;
                success = false;
            }

            Timestamps.Add(new VLXTimestamp()
            {
                ID = TimestampID,
                StartTime = startTime,
                EndTime = endTime
            });
 
            return success;
        }

        public void UpdateCategoryInfo(WrapSQLite sql)
        {
            sql.Open();
            sql.ExecuteNonQuery($"UPDATE {VLXDB.Category.Self} SET {VLXDB.Category.Name} = ?, {VLXDB.Category.Description} = ?, {VLXDB.Category.Color} = ? WHERE {VLXDB.Category.ID} = ?", Name, Description, CategoryColor.ToArgb(), ID);
            sql.Close();
        }


        private int GetIso8601WeekOfYear(DateTime time)
        {
            DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday) time = time.AddDays(3);
            return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(time, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        public DateTime FirstDateOfWeekISO8601(int year, int weekOfYear)
        {
            DateTime jan1 = new DateTime(year, 1, 1);
            int daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;

            DateTime firstThursday = jan1.AddDays(daysOffset);
            var cal = CultureInfo.CurrentCulture.Calendar;
            int firstWeek = cal.GetWeekOfYear(firstThursday, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

            var weekNum = weekOfYear;

            if (firstWeek == 1) weekNum -= 1;

            var result = firstThursday.AddDays(weekNum * 7);

            return result.AddDays(-3);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
