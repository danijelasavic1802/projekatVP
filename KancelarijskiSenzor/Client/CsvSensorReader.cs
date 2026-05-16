using Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Client
{
    public class CsvSensorReader : IDisposable
    {
        private StreamReader reader;
        private bool disposed = false;

        public string Path { get; private set; }

        public CsvSensorReader(string path)
        {
            Path = path;

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("CSV fajl nije pronađen.", path);
            }

            reader = new StreamReader(path);
        }

        public List<SensorSample> ReadFirstSamples(int count, RejectLogger logger)
        {
            List<SensorSample> samples = new List<SensorSample>();
            string line;
            int lineNumber = 0;

            reader.ReadLine();

            while ((line = reader.ReadLine()) != null)
            {
                lineNumber++;

                if (samples.Count >= count)
                {
                    logger.Log(lineNumber, line, "Red je višak jer se učitava samo prvih 130 redova.");
                    continue;
                }

                try
                {
                    SensorSample sample = ParseLine(line);
                    samples.Add(sample);
                }
                catch (Exception ex)
                {
                    logger.Log(lineNumber, line, ex.Message);
                }
            }

            return samples;
        }

        private SensorSample ParseLine(string line)
        {
            string[] parts = line.Split(',');
            if (parts.Length < 8)
            {
                throw new FormatException("Red nema dovoljno kolona.");
            }

            return new SensorSample
            {
                DateTime = DateTime.Parse(parts[0], CultureInfo.InvariantCulture),
                Volume = double.Parse(parts[1], CultureInfo.InvariantCulture),
                LightLevel = double.Parse(parts[2], CultureInfo.InvariantCulture),
                RelativeHumidity = double.Parse(parts[6], CultureInfo.InvariantCulture),
                AirQuality = double.Parse(parts[7], CultureInfo.InvariantCulture)
            };
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~CsvSensorReader()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing && reader != null)
                {
                    reader.Dispose();
                    reader = null;
                }

                disposed = true;
            }
        }
    }
}