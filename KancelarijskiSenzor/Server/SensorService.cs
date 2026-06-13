using Common;
using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.ServiceModel;

namespace Server
{
    public class SensorService : ISensorService
    {
        private static bool sessionStarted = false;

        private static StreamWriter measurementsWriter;
        private static StreamWriter rejectsWriter;

        private static SensorSample previousSample = null;

        private static int sampleCount = 0;
        private static double lightSum = 0;

        private static readonly object locker = new object();

        private static bool eventsSubscribed = false;

        public static event EventHandler OnTransferStarted;
        public static event EventHandler<SensorSampleEventArgs> OnSampleReceived;
        public static event EventHandler OnTransferCompleted;
        public static event EventHandler<SensorWarningEventArgs> OnWarningRaised;

        public SensorService()
        {
            if (!eventsSubscribed)
            {
                OnTransferStarted += TransferStartedHandler;
                OnSampleReceived += SampleReceivedHandler;
                OnTransferCompleted += TransferCompletedHandler;
                OnWarningRaised += WarningRaisedHandler;

                eventsSubscribed = true;
            }
        }

        public ServiceResponse StartSession(SensorSample meta)
        {
            ValidateSample(meta);

            lock (locker)
            {
                sessionStarted = true;
                previousSample = null;
                sampleCount = 0;
                lightSum = 0;

                CreateSessionFiles();

                OnTransferStarted?.Invoke(this, EventArgs.Empty);

                Console.WriteLine("Sesija je započeta.");
                Console.WriteLine("Prenos u toku...");
            }

            return new ServiceResponse
            {
                Ack = true,
                Message = "StartSession uspešan.",
                Status = SessionStatus.IN_PROGRESS
            };
        }

        public ServiceResponse PushSample(SensorSample sample)
        {
            lock (locker)
            {
                if (!sessionStarted)
                {
                    throw new FaultException<ValidationFault>(
                        new ValidationFault("Sesija nije započeta. Prvo pozvati StartSession.")
                    );
                }

                try
                {
                    ValidateSample(sample);
                }
                catch (FaultException<ValidationFault> ex)
                {
                    WriteReject(sample, ex.Detail.Message);
                    throw;
                }
                catch (FaultException<DataFormatFault> ex)
                {
                    WriteReject(sample, ex.Detail.Message);
                    throw;
                }

                WriteMeasurement(sample);

                Console.WriteLine("Prenos u toku...");
                Console.WriteLine($"Primljen uzorak: L={sample.LightLevel}, RH={sample.RelativeHumidity}, AQ={sample.AirQuality}");

                OnSampleReceived?.Invoke(this, new SensorSampleEventArgs(sample));

                AnalyzeSample(sample);

                previousSample = sample;
                sampleCount++;
                lightSum += sample.LightLevel;
            }

            return new ServiceResponse
            {
                Ack = true,
                Message = "Uzorak uspešno primljen.",
                Status = SessionStatus.IN_PROGRESS
            };
        }

        public ServiceResponse EndSession()
        {
            lock (locker)
            {
                sessionStarted = false;

                CloseWriters();

                OnTransferCompleted?.Invoke(this, EventArgs.Empty);

                Console.WriteLine("Završen prenos.");
                Console.WriteLine("Sesija je završena.");
            }

            return new ServiceResponse
            {
                Ack = true,
                Message = "EndSession uspešan.",
                Status = SessionStatus.COMPLETED
            };
        }

        private static void CreateSessionFiles()
        {
            string outputFolder = ConfigurationManager.AppSettings["ServerOutputFolder"];

            if (string.IsNullOrWhiteSpace(outputFolder))
            {
                outputFolder = "ServerData";
            }

            string sessionFolder = Path.Combine(
                outputFolder,
                "Session_" + DateTime.Now.ToString("yyyyMMdd_HHmmss")
            );

            Directory.CreateDirectory(sessionFolder);

            string measurementsPath = Path.Combine(sessionFolder, "measurements_session.csv");
            string rejectsPath = Path.Combine(sessionFolder, "rejects.csv");

            measurementsWriter = new StreamWriter(measurementsPath, false);
            rejectsWriter = new StreamWriter(rejectsPath, false);

            measurementsWriter.WriteLine("DateTime,Volume,RelativeHumidity,AirQuality,LightLevel");
            rejectsWriter.WriteLine("DateTime,Volume,RelativeHumidity,AirQuality,LightLevel,Reason");

            measurementsWriter.Flush();
            rejectsWriter.Flush();
        }

        private static void WriteMeasurement(SensorSample sample)
        {
            measurementsWriter.WriteLine(ToCsvLine(sample));
            measurementsWriter.Flush();
        }

        private static void WriteReject(SensorSample sample, string reason)
        {
            if (rejectsWriter == null)
            {
                return;
            }

            string line = sample == null
                ? ",,,,,Sample je null"
                : ToCsvLine(sample) + ",\"" + reason + "\"";

            rejectsWriter.WriteLine(line);
            rejectsWriter.Flush();
        }

        private static string ToCsvLine(SensorSample sample)
        {
            return string.Join(",",
                sample.DateTime.ToString("o", CultureInfo.InvariantCulture),
                sample.Volume.ToString(CultureInfo.InvariantCulture),
                sample.RelativeHumidity.ToString(CultureInfo.InvariantCulture),
                sample.AirQuality.ToString(CultureInfo.InvariantCulture),
                sample.LightLevel.ToString(CultureInfo.InvariantCulture)
            );
        }

        private static void AnalyzeSample(SensorSample sample)
        {
            double lThreshold = ReadDoubleFromConfig("L_threshold", 50);
            double rhThreshold = ReadDoubleFromConfig("RH_threshold", 10);
            double aqThreshold = ReadDoubleFromConfig("AQ_threshold", 20);
            double deviationPercent = ReadDoubleFromConfig("AverageDeviationPercent", 25);

            if (previousSample != null)
            {
                double deltaL = sample.LightLevel - previousSample.LightLevel;

                if (Math.Abs(deltaL) > lThreshold)
                {
                    string direction = deltaL > 0 ? "iznad očekivanog" : "ispod očekivanog";

                    RaiseWarning(
                        "LightSpike",
                        direction,
                        sample.LightLevel,
                        previousSample.LightLevel,
                        deltaL,
                        sample.DateTime
                    );
                }

                double deltaRH = sample.RelativeHumidity - previousSample.RelativeHumidity;

                if (Math.Abs(deltaRH) > rhThreshold)
                {
                    string direction = deltaRH > 0 ? "iznad očekivanog" : "ispod očekivanog";

                    RaiseWarning(
                        "RHSpike",
                        direction,
                        sample.RelativeHumidity,
                        previousSample.RelativeHumidity,
                        deltaRH,
                        sample.DateTime
                    );
                }

                double deltaAQ = sample.AirQuality - previousSample.AirQuality;

                if (Math.Abs(deltaAQ) > aqThreshold)
                {
                    string direction = deltaAQ > 0 ? "iznad očekivanog" : "ispod očekivanog";

                    RaiseWarning(
                        "AQSpike",
                        direction,
                        sample.AirQuality,
                        previousSample.AirQuality,
                        deltaAQ,
                        sample.DateTime
                    );
                }
            }

            if (sampleCount > 0)
            {
                double lightMean = lightSum / sampleCount;

                double lowerLimit = lightMean * (1 - deviationPercent / 100);
                double upperLimit = lightMean * (1 + deviationPercent / 100);

                if (sample.LightLevel < lowerLimit || sample.LightLevel > upperLimit)
                {
                    string direction = sample.LightLevel > upperLimit
                        ? "iznad očekivane vrednosti"
                        : "ispod očekivane vrednosti";

                    RaiseWarning(
                        "OutOfBandWarning",
                        direction,
                        sample.LightLevel,
                        lightMean,
                        sample.LightLevel - lightMean,
                        sample.DateTime
                    );
                }
            }
        }

        private static void RaiseWarning(
            string warningType,
            string direction,
            double currentValue,
            double expectedValue,
            double difference,
            DateTime dateTime)
        {
            OnWarningRaised?.Invoke(
                null,
                new SensorWarningEventArgs(
                    warningType,
                    direction,
                    currentValue,
                    expectedValue,
                    difference,
                    dateTime
                )
            );
        }

        private static double ReadDoubleFromConfig(string key, double defaultValue)
        {
            string value = ConfigurationManager.AppSettings[key];

            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }

            return defaultValue;
        }

        private static void CloseWriters()
        {
            if (measurementsWriter != null)
            {
                measurementsWriter.Flush();
                measurementsWriter.Dispose();
                measurementsWriter = null;
            }

            if (rejectsWriter != null)
            {
                rejectsWriter.Flush();
                rejectsWriter.Dispose();
                rejectsWriter = null;
            }
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

        private static void TransferStartedHandler(object sender, EventArgs e)
        {
            Console.WriteLine("[EVENT] OnTransferStarted - započet prenos podataka.");
        }

        private static void SampleReceivedHandler(object sender, SensorSampleEventArgs e)
        {
            Console.WriteLine("[EVENT] OnSampleReceived - primljen je novi uzorak.");
        }

        private static void TransferCompletedHandler(object sender, EventArgs e)
        {
            Console.WriteLine("[EVENT] OnTransferCompleted - prenos je završen.");
        }

        private static void WarningRaisedHandler(object sender, SensorWarningEventArgs e)
        {
            Console.WriteLine(
                $"[WARNING] {e.WarningType}: {e.Direction}. " +
                $"Trenutna vrednost={e.CurrentValue}, očekivana/prethodna={e.ExpectedValue}, " +
                $"razlika={e.Difference}, vreme={e.DateTime}"
            );
        }
    }
}