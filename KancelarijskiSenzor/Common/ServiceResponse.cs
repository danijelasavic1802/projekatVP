using System.Runtime.Serialization;

namespace Common
{
    [DataContract]
    public class ServiceResponse
    {
        [DataMember]
        public bool Ack { get; set; }

        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public SessionStatus Status { get; set; }
    }
}