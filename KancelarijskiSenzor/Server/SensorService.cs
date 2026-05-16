using Common;
using System;
using System.ServiceModel;

namespace Server
{
    public class SensorService : ISensorService
    {
        private static bool sessionStarted = false;

        public ServiceResponse StartSession(SensorSample meta)
        {
            ValidateSample(meta);

            sessionStarted = true;

            Console.WriteLine("Sesija je započeta.");

            return new ServiceResponse
            {
                Ack = true,
                Message = "StartSession uspešan.",
                Status = SessionStatus.IN_PROGRESS
            };
        }

        public ServiceResponse PushSample(SensorSample sample)
        {
            if (!sessionStarted)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("Sesija nije započeta. Prvo pozvati StartSession.")
                );
            }

            ValidateSample(sample);

            Console.WriteLine($"Primljen uzorak: L={sample.LightLevel}, RH={sample.RelativeHumidity}, AQ={sample.AirQuality}");

            return new ServiceResponse
            {
                Ack = true,
                Message = "Uzorak uspešno primljen.",
                Status = SessionStatus.IN_PROGRESS
            };
        }

        public ServiceResponse EndSession()
        {
            sessionStarted = false;

            Console.WriteLine("Sesija je završena.");

            return new ServiceResponse
            {
                Ack = true,
                Message = "EndSession uspešan.",
                Status = SessionStatus.COMPLETED
            };
        }

        private void ValidateSample(SensorSample sample)
        {
            if (sample == null)
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("Sample ne sme biti null.")
                );
            }

            if (sample.DateTime == default(DateTime))
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("DateTime je obavezno polje.")
                );
            }

            if (sample.RelativeHumidity <= 0 || sample.RelativeHumidity > 100)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("RelativeHumidity mora biti u opsegu 0-100.")
                );
            }

            if (sample.LightLevel < 0)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("LightLevel ne sme biti negativan.")
                );
            }

            if (sample.AirQuality < 0)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("AirQuality ne sme biti negativan.")
                );
            }

            if (sample.Volume < 0)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("Volume ne sme biti negativan.")
                );
            }
        }
    }
}