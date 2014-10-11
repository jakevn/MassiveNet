// Copyright 2014 - Inhumane Software - legal@inhumanesoftware.com

using System;
using System.Diagnostics;

namespace MassiveNet {
    /// <summary>
    /// NetTime provides 
    /// </summary>
    public class NetTime {
        private static readonly long Start = Stopwatch.GetTimestamp();
        private static readonly DateTime DtStart = DateTime.Now;
        private static readonly double Freq = 1.0/(double) Stopwatch.Frequency;

        /// <summary>
        /// High-precision time represented as elapsed milliseconds.
        /// </summary>
        public static uint Milliseconds() {
            long diff = Stopwatch.GetTimestamp() - Start;
            double seconds = (double) diff*Freq;
            return (uint) (seconds*1000.0);
        }

        /// <summary>
        /// High-precision time represented as elapsed seconds.
        /// </summary>
        public static uint Seconds() {
            long diff = Stopwatch.GetTimestamp() - Start;
            double seconds = (uint) diff*Freq;
            return (uint) seconds;
        }

        /// <summary>
        /// Returns the difference (in milliseconds) between the supplied time and current time.
        /// </summary>
        public static uint ElapsedMilliseconds(uint startMilliseconds) {
            return Milliseconds() - startMilliseconds;
        }

        /// <summary>
        /// Returns the difference (in seconds) between the supplied time and current time.
        /// </summary>
        public static uint ElapsedSeconds(uint startSeconds) {
            return Seconds() - startSeconds;
        }

        /// <summary>
        /// Returns the DateTime representing the socket start time. The time is not precise.
        /// </summary>
        public static DateTime StartDateTime() {
            return DtStart;
        }

        /// <summary>
        /// Returns a new DateTime representing the current local time. The time is not precise and creates garbage.
        /// Only useful for pretty printing the time (e.g., console output, logging).
        /// </summary>
        public static DateTime CurrentDateTime() {
            return DateTime.Now;
        }
    }
}