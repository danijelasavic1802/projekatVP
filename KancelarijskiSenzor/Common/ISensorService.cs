using System.ServiceModel;

namespace Common
{
    [ServiceContract]
    public interface ISensorService
    {
        [OperationContract]
        [FaultContract(typeof(DataFormatFault))]
        [FaultContract(typeof(ValidationFault))]
        ServiceResponse StartSession(SensorSample meta);

        [OperationContract]
        [FaultContract(typeof(DataFormatFault))]
        [FaultContract(typeof(ValidationFault))]
        ServiceResponse PushSample(SensorSample sample);

        [OperationContract]
        ServiceResponse EndSession();
    }
}