using Common;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.ServiceModel;
using System.Threading;

namespace Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string csvPath = ConfigurationManager.AppSettings["CsvPath"];
            string rejectLogPath = ConfigurationManager.AppSettings["RejectLogPath"];

            List<SensorSample> samples;

            try
            {
                using (RejectLogger logger = new RejectLogger(rejectLogPath))
                using (CsvSensorReader reader = new CsvSensorReader(csvPath))
                {
                    samples = reader.ReadFirstSamples(130, logger);
                }

                Console.WriteLine($"Učitano ispravnih redova: {samples.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Greška pri čitanju CSV fajla: " + ex.Message);
                Console.ReadLine();
                return;
            }

            ChannelFactory<ISensorService> factory =
                new ChannelFactory<ISensorService>("SensorServiceEndpoint");

            ISensorService proxy = factory.CreateChannel();

            try
            {
                if (samples.Count == 0)
                {
                    Console.WriteLine("Nema ispravnih redova za slanje.");
                    Console.ReadLine();
                    return;
                }

                ServiceResponse startResponse = proxy.StartSession(samples[0]);
                Console.WriteLine($"StartSession: ACK={startResponse.Ack}, Status={startResponse.Status}");

                for (int i = 0; i < samples.Count; i++)
                {
                    ServiceResponse response = proxy.PushSample(samples[i]);
                    Console.WriteLine($"Red {i + 1}: ACK={response.Ack}, Status={response.Status}");

                    Thread.Sleep(100);
                }

                ServiceResponse endResponse = proxy.EndSession();
                Console.WriteLine($"EndSession: ACK={endResponse.Ack}, Status={endResponse.Status}");

                ((IClientChannel)proxy).Close();
                factory.Close();
            }
            catch (FaultException<DataFormatFault> ex)
            {
                Console.WriteLine("DataFormatFault: " + ex.Detail.Message);
                ((IClientChannel)proxy).Abort();
                factory.Abort();
            }
            catch (FaultException<ValidationFault> ex)
            {
                Console.WriteLine("ValidationFault: " + ex.Detail.Message);
                ((IClientChannel)proxy).Abort();
                factory.Abort();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Greška u komunikaciji: " + ex.Message);
                ((IClientChannel)proxy).Abort();
                factory.Abort();
            }

            Console.WriteLine("Pritisni ENTER za kraj.");
            Console.ReadLine();
        }
    }
}