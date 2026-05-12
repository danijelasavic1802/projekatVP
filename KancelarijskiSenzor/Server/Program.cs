using System;
using System.ServiceModel;

namespace Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ServiceHost host = new ServiceHost(typeof(SensorService));

            try
            {
                host.Open();

                Console.WriteLine("WCF server je pokrenut.");
                Console.WriteLine("Adresa: net.tcp://localhost:9000/SensorService");
                Console.WriteLine("Pritisni ENTER za gašenje servera.");

                Console.ReadLine();

                host.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Greška na serveru: " + ex.Message);
                host.Abort();
            }
        }
    }
}