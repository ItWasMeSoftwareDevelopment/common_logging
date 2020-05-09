using NLog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ItWasMe.CommonLogging
{
    public interface ILoggingContext
    {
        Logger CreateLogger<T>();

        void SetProperty(string @prop, string value);
        Task SetPropertyAsync(string @prop, string value);
    }
    public class LoggingContext : ILoggingContext
    {
        private readonly Dictionary<string, string> _properties;
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly List<Logger> _loggers = new List<Logger>();
        private readonly string _id = Guid.NewGuid().ToString("N");

        public LoggingContext(Dictionary<string, string> properties)
        {
            _properties = properties;
        }

        public Logger CreateLogger<T>()
        {
            var logger = LogManager.GetLogger(typeof(T).FullName + _id);
            _loggers.Add(logger);
            foreach (var p in _properties)
            {
                logger.SetProperty(p.Key, p.Value);
            }

            return logger;
        }

        public void SetProperty(string prop, string value)
        {
            try
            {
                _semaphoreSlim.Wait();
                AddOrUpdateProperty(prop, value);
                foreach (var logger in _loggers)
                {
                    logger.SetProperty(prop, value);
                }
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task SetPropertyAsync(string prop, string value)
        {
            try
            {
                await _semaphoreSlim.WaitAsync();
                AddOrUpdateProperty(prop, value);
                foreach (var logger in _loggers)
                {
                    logger.SetProperty(prop, value);
                }
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        private void AddOrUpdateProperty(string prop, string value)
        {
            if (_properties.ContainsKey(prop))
            {
                _properties[prop] = value;
            }
            else
            {
                _properties.Add(prop, value);
            }
        }
    }
}
