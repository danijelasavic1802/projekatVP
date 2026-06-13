using System;

namespace Server
{
    public class SensorWarningEventArgs : EventArgs
    {
        public string WarningType { get; private set; }
        public string Direction { get; private set; }
        public double CurrentValue { get; private set; }
        public double ExpectedValue { get; private set; }
        public double Difference { get; private set; }
        public DateTime DateTime { get; private set; }

        public SensorWarningEventArgs(
            string warningType,
            string direction,
            double currentValue,
            double expectedValue,
            double difference,
            DateTime dateTime)
        {
            WarningType = warningType;
            Direction = direction;
            CurrentValue = currentValue;
            ExpectedValue = expectedValue;
            Difference = difference;
            DateTime = dateTime;
        }
    }
}