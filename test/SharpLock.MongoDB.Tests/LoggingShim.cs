using System;

namespace SharpLock.MongoDB.Tests
{
    public class LoggingShim : ILogger
    {
        private readonly Serilog.ILogger _logger;

        public LoggingShim(Serilog.ILogger logger)
        {
            _logger = logger;
        }

        public void Critical(string message)
        {
            _logger.Fatal(message);
        }

        public void Critical(Exception ex)
        {
            _logger.Fatal(ex, string.Empty);
        }

        public void Critical(Exception ex, string message)
        {
            _logger.Fatal(ex, message);
        }

        public void Debug(string message, params object[] objs)
        {
            _logger.Debug(message, objs);
        }

        public void Debug(Exception ex)
        {
            _logger.Debug(ex, string.Empty);
        }

        public void Debug(Exception ex, string message)
        {
            _logger.Debug(ex, message);
        }

        public void Error(string message)
        {
            _logger.Error(message);
        }

        public void Error(Exception ex)
        {
            _logger.Error(ex, string.Empty);
        }

        public void Error(Exception ex, string message)
        {
            _logger.Error(ex, message);
        }

        public void Information(string message)
        {
            _logger.Information(message);
        }

        public void Information(Exception ex)
        {
            _logger.Information(ex, string.Empty);
        }

        public void Information(Exception ex, string message)
        {
            _logger.Information(ex, message);
        }

        public void Trace(string message, params object[] objs)
        {
            _logger.Verbose(message, objs);
        }

        public void Trace(Exception ex)
        {
            _logger.Verbose(ex, string.Empty);
        }

        public void Trace(Exception ex, string message)
        {
            _logger.Verbose(ex, message);
        }

        public void Warn(string message)
        {
            _logger.Warning(message);
        }

        public void Warn(Exception ex)
        {
            _logger.Warning(ex, string.Empty);
        }

        public void Warn(Exception ex, string message)
        {
            _logger.Warning(ex, message);
        }
    }
}
