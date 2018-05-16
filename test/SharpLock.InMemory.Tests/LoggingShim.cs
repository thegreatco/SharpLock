using System;

namespace SharpLock.InMemory.Tests
{
    public class LoggingShim : ISharpLockLogger
    {
        public void Critical(string message)
        {
            Console.WriteLine(message);
        }

        public void Critical(Exception ex)
        {
            Console.WriteLine(ex);
        }

        public void Critical(Exception ex, string message)
        {
            Console.WriteLine($"{message} - {ex}");
        }

        public void Debug(string message, params object[] objs)
        {
            Console.WriteLine(message, objs);
        }

        public void Debug(Exception ex)
        {
            Console.WriteLine(ex);
        }

        public void Debug(Exception ex, string message)
        {
            Console.WriteLine($"{message} - {ex}");
        }

        public void Error(string message)
        {
            Console.WriteLine(message);
        }

        public void Error(Exception ex)
        {
            Console.WriteLine(ex);
        }

        public void Error(Exception ex, string message)
        {
            Console.WriteLine($"{message} - {ex}");
        }

        public void Information(string message)
        {
            Console.WriteLine(message);
        }

        public void Information(Exception ex)
        {
            Console.WriteLine(ex);
        }

        public void Information(Exception ex, string message)
        {
            Console.WriteLine($"{message} - {ex}");
        }

        public void Trace(string message, params object[] objs)
        {
            Console.WriteLine(message, objs);
        }

        public void Trace(Exception ex)
        {
            Console.WriteLine(ex);
        }

        public void Trace(Exception ex, string message)
        {
            Console.WriteLine($"{message} - {ex}");
        }

        public void Warn(string message)
        {
            Console.WriteLine(message);
        }

        public void Warn(Exception ex)
        {
            Console.WriteLine(ex);
        }

        public void Warn(Exception ex, string message)
        {
            Console.WriteLine($"{message} - {ex}");
        }
    }
}
