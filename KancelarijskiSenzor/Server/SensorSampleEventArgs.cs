using Common;
using System;

namespace Server
{
    public class SensorSampleEventArgs : EventArgs
    {
        public SensorSample Sample { get; private set; }

        public SensorSampleEventArgs(SensorSample sample)
        {
            Sample = sample;
        }
    }
}