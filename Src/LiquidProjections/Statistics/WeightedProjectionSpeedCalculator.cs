using System;
using System.Collections.Concurrent;
using System.Linq;

namespace LiquidProjections.Statistics
{
    /// <summary>
    /// Calculates the weighted speed in transactions per second.
    /// </summary>
    public class WeightedProjectionSpeedCalculator
    {
        private readonly object syncObject = new object();

        private readonly TimeSpan threshold;

        private readonly int maxNrOfSamples;

        private readonly ConcurrentQueue<float> samples = new ConcurrentQueue<float>();
        private long lastCheckpoint;
        private DateTime lastSampleTimeStampUtc;
        private volatile bool hasBaselineBeenSet;

        public WeightedProjectionSpeedCalculator(int maxNrOfSamples, TimeSpan threshold)
        {
            this.maxNrOfSamples = maxNrOfSamples;
            this.threshold = threshold;
        }

        public DateTime LastSampleTimeStampUtc
        {
            get
            {
                lock (syncObject)
                {
                    return lastSampleTimeStampUtc;
                }
            }
            private set
            {
                lock (syncObject)
                {
                    lastSampleTimeStampUtc = value;
                }
            }
        }

        public long LastCheckpoint
        {
            get
            {
                lock (syncObject)
                {
                    return lastCheckpoint;
                }
            }
            private set
            {
                lock (syncObject)
                {
                    lastCheckpoint = value;
                }
            }
        }

        public bool HasBaselineBeenSet
        {
            get { return hasBaselineBeenSet; }
        }

        public void Record(long checkpoint, DateTime timestampUtc)
        {
            lock (syncObject)
            {
                TimeSpan interval = timestampUtc - LastSampleTimeStampUtc;
                if (interval > threshold)
                {
                    long delta = checkpoint - LastCheckpoint;

                    samples.Enqueue((float)(delta / interval.TotalSeconds));

                    LastCheckpoint = checkpoint;
                    LastSampleTimeStampUtc = timestampUtc;

                    DiscardOlderSamples();
                }
            }
        }
   
        private void DiscardOlderSamples()
        {
            while (samples.Count > maxNrOfSamples)
            {
                float discardedItem;
                samples.TryDequeue(out discardedItem);
            }
        }

        public float GetWeightedSpeedIncluding(float sample)
        {
            return GetWeightedSpeed(samples.ToArray().Concat(new[] {sample}).ToArray());
        }

        public float GetWeightedSpeed()
        {
            return GetWeightedSpeed(samples.ToArray());
        }

        private float GetWeightedSpeed(float[] snapshot)
        {
            if (snapshot.Length == 0)
            {
                return 0;
            }

            float weightedSum = 0;
            int weights = 0;

            for (int index = 0; index < snapshot.Length; index++)
            {
                weights += index + 1;
                weightedSum += snapshot[index] * (index + 1);
            }

            return weightedSum / weights;
        }

        public void SetBaseline(long checkpoint, DateTime timestampUtc)
        {
            lock (syncObject)
            {
                lastCheckpoint = checkpoint;
                LastSampleTimeStampUtc = timestampUtc;

                hasBaselineBeenSet = true;
            }
        }
    }
}