using System;
using System.IO;

namespace Client
{
    public class RejectLogger : IDisposable
    {
        private StreamWriter writer;
        private bool disposed = false;

        public RejectLogger(string path)
        {
            writer = new StreamWriter(path, true);
            writer.WriteLine("LineNumber,OriginalLine,Reason");
        }

        public void Log(int lineNumber, string originalLine, string reason)
        {
            writer.WriteLine($"{lineNumber},\"{originalLine}\",\"{reason}\"");
            writer.Flush();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~RejectLogger()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing && writer != null)
                {
                    writer.Dispose();
                    writer = null;
                }

                disposed = true;
            }
        }
    }
}