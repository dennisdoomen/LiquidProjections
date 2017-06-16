using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace LiquidProjections.Statistics
{
    /// <summary>
    /// Contains statistics and information about a particular projector.
    /// </summary>
    /// <remarks>
    /// An instance of this class is safe for use in multi-threaded solutions.
    /// </remarks>
    public class ProjectorStats
    {
        private readonly object eventsSyncObject = new object();
        private readonly object progressSyncObject = new object();

        private readonly ConcurrentDictionary<string, Property> properties =
            new ConcurrentDictionary<string, Property>();

        private readonly List<Event> events = new List<Event>();

        private readonly WeightedProjectionSpeedCalculator lastMinuteSamples =
            new WeightedProjectionSpeedCalculator(12, TimeSpan.FromSeconds(5));

        private readonly WeightedProjectionSpeedCalculator last10MinuteSamples
            = new WeightedProjectionSpeedCalculator(9, TimeSpan.FromMinutes(1));

        public ProjectorStats(string projectorId)
        {
            ProjectorId = projectorId;
        }

        public ProjectorStats(string projectorId, IDictionary<string, Property> properties, IEnumerable<Event> events)
        {
            this.properties = new ConcurrentDictionary<string, Property>(properties);

            foreach (Event @event in events)
            {
                this.events.Add(@event);
            }

            ProjectorId = projectorId;
        }

        public string ProjectorId { get; }

        public TimestampedCheckpoint LastCheckpoint { get; set; }

        /// <summary>
        /// Gets a snapshot of the properties stored for this projector at the time of calling.
        /// </summary>
        public IDictionary<string, Property> GetProperties() => properties.ToArray().ToDictionary(p => p.Key, p => p.Value);

        /// <summary>
        /// Gets a snapshot of the events stored for this projector at the time of calling.
        /// </summary>
        public IReadOnlyList<Event> GetEvents()
        {
            lock (eventsSyncObject)
            {
                return events.ToReadOnly();
            }
        }

        public void StoreProperty(string key, string value, DateTime timestampUtc)
        {
            properties[key] = new Property(value, timestampUtc);
        }

        public void LogEvent(string body, DateTime timestampUtc)
        {
            lock (eventsSyncObject)
            {
                events.Add(new Event(body, timestampUtc));
            }
        }

        /// <summary>
        /// Calculates the expected time for a projector to reach a certain <paramref name="targetCheckpoint"/> based 
        /// on a weighted average over the last ten minutes, or <c>null</c> if there is not enough information yet.
        /// </summary>
        public TimeSpan? GetTimeToReach(long targetCheckpoint)
        {
            lock (progressSyncObject)
            {
                float speed = lastMinuteSamples.GetWeightedSpeed();

                speed = (speed == 0)
                    ? last10MinuteSamples.GetWeightedSpeed()
                    : last10MinuteSamples.GetWeightedSpeedIncluding(speed);

                if (speed > 0)
                {
                    long seconds = (long) ((targetCheckpoint - LastCheckpoint.Checkpoint) / speed);
                    return TimeSpan.FromSeconds(seconds);
                }
                else
                {
                    return null;
                }
                
            }
        }

        public void TrackProgress(long checkpoint, DateTime timestampUtc)
        {
            lock (progressSyncObject)
            {
                if (!lastMinuteSamples.HasBaselineBeenSet)
                {
                    lastMinuteSamples.SetBaseline(checkpoint, timestampUtc);
                    last10MinuteSamples.SetBaseline(checkpoint, timestampUtc);
                }

                lastMinuteSamples.Record(checkpoint, timestampUtc);
                last10MinuteSamples.Record(checkpoint, timestampUtc);

                LastCheckpoint = new TimestampedCheckpoint(checkpoint, timestampUtc);
            }
        }
    }
}