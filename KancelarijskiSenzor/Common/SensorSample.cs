using System;
using System.Runtime.Serialization;

namespace Common
{
    [DataContract]
    public class SensorSample
    {
        [DataMember]
        public double Volume { get; set; }

        [DataMember]
        public double RelativeHumidity { get; set; }

        [DataMember]
        public double AirQuality { get; set; }

        [DataMember]
        public double LightLevel { get; set; }

        [DataMember]
        public DateTime DateTime { get; set; }
    }
}