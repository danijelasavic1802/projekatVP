using System.Runtime.Serialization;

namespace Common
{
    [DataContract]
    public enum SessionStatus
    {
        [EnumMember]
        IN_PROGRESS,

        [EnumMember]
        COMPLETED
    }
}