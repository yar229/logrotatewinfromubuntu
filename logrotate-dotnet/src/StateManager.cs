using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LogRotate
{
    /// <summary>
    /// Mirrors the C struct logState. The LastRotated field keeps the same
    /// meaning as the C struct tm (year-1900, month 0-11).
    /// </summary>
    public sealed class LogState
    {
        public string Fn { get; set; } = string.Empty;
        public RotatedTime LastRotated { get; set; } = new RotatedTime();
        public FileStat? Sb { get; set; }
        public bool DoRotate;
        public bool IsUsed;  /* True if there is real log file in system for this state. */
    }

    /// <summary>
    /// Holds the fields of struct tm that logrotate uses: y(mon), mday, hour,
    /// min, sec, wday, isdst.
    /// </summary>
    public sealed class RotatedTime
    {
        public int Year;   /* years since 1900 (as in C tm_year) */
        public int Mon;    /* 0-11 (as in C tm_mon) */
        public int MDay;
        public int Hour;
        public int Min;
        public int Sec;
        public int WDay;   /* 0=Sunday */
        public int IsDst = -1;

        public DateTime ToDateTime()
        {
            return new DateTime(Year + 1900, Mon + 1, MDay, Hour, Min, Sec, DateTimeKind.Local);
        }

        public static RotatedTime FromDateTime(DateTime dt)
        {
            return new RotatedTime
            {
                Year = dt.Year - 1900,
                Mon = dt.Month - 1,
                MDay = dt.Day,
                Hour = dt.Hour,
                Min = dt.Minute,
                Sec = dt.Second,
                WDay = (int)dt.DayOfWeek,
            };
        }
    }

    /// <summary>
    /// State file management: read, write, lock (port of lockState/readState/writeState).
    /// </summary>
    public sealed class StateManager
    {
        private readonly List<List<LogState>> _states = new List<List<LogState>>();

        public const int HASH_SIZE_MIN = 64;
        public const int HASH_SIZE_MAX = 8192;

        public int HashSize => _states.Count;

        /// <summary>
        /// "now" used when creating brand new states (replaces the C global
        /// nowSecs/local time).
        /// </summary>
        public static DateTime CurrentTime { get; set; } = DateTime.Now;

        public void AllocateHash(long hs)
        {
            if (hs < HASH_SIZE_MIN) hs = HASH_SIZE_MIN;
            if (hs > HASH_SIZE_MAX) hs = HASH_SIZE_MAX;

            Log.Message(MESS.DEBUG, "Allocating hash table for state file, size {0} entries\n", hs);
            _states.Clear();
            for (int i = 0; i < hs; i++)
                _states.Add(new List<LogState>());
        }

        private static int HashIndex(string fn, int size)
        {
            unchecked
            {
                uint hash = 0;
                foreach (var c in fn)
                {
                    hash *= 13;
                    hash += (byte)c;
                }
                return (int)(hash % (uint)size);
            }
        }

        /// <summary>
        /// Finds (creating if needed) a state for the given filename,
        /// using the C-style hash (findState).
        /// </summary>
        public LogState FindState(string fn)
        {
            return FindStateImpl(fn, _states.Count);
        }

        /// <summary>
        /// Linear scan used by readState while populating the state file
        /// (port of findState2). size == 0 means the hash table is not valid yet.
        /// </summary>
        public LogState FindState2(string fn, int size)
        {
            if (size > 0)
            {
                return FindStateImpl(fn, size);
            }
            foreach (var list in _states)
            {
                foreach (var s in list)
                {
                    if (s.Fn == fn)
                        return s;
                }
            }
            return FindStateImpl(fn, _states.Count);
        }

        private LogState FindStateImpl(string fn, int size)
        {
            int idx = HashIndex(fn, size);
            foreach (var s in _states[idx])
            {
                if (s.Fn == fn)
                    return s;
            }

            Log.Message(MESS.DEBUG, "Creating new state\n");
            var st = new LogState { Fn = fn };
            var now = CurrentTime;
            // port of newState(): lastRotated initialized to now (hour/mday/mon/year),
            // minute/second zeroed, wday from current time.
            st.LastRotated = new RotatedTime
            {
                Year = 0, //now.Year - 1900, //me
                Mon = 0, //now.Month - 1,
                MDay = 0, //now.Day,
                Hour = 0, //now.Hour,
                Min = 0,
                Sec = 0,
                WDay = 0, //(int)now.DayOfWeek,
                IsDst = -1,
            };
            _states[idx].Add(st);
            return st;
        }

        public IEnumerable<LogState> AllStates()
        {
            foreach (var list in _states)
                foreach (var s in list)
                    yield return s;
        }

        public IEnumerable<List<LogState>> Buckets => _states;
    }
}