using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hypernex.CCK;
using Hypernex.Configuration;
using Hypernex.Tools;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VRCFaceTracking.Core.Contracts.Services;

namespace Hypernex.ExtendedTracking
{
    public static class FaceTrackingServices
    {
        public class FTLogger : ILogger
        {
            private string p;

            public FTLogger(string c) => p = $"[FT][{p}] ";
            
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                switch (logLevel)
                {
                    case LogLevel.Warning:
                        Logger.CurrentLogger.Warn(state.ToString());
                        break;
                    case LogLevel.Error:
                        Logger.CurrentLogger.Error(state.ToString());
                        break;
                    case LogLevel.Critical:
                        Logger.CurrentLogger.Critical(exception);
                        break;
                    default:
                        Logger.CurrentLogger.Log(state.ToString());
                        break;
                }
            }

            public bool IsEnabled(LogLevel logLevel) => true;

            public IDisposable BeginScope<TState>(TState state) where TState : notnull => new _();
        }
        
        private class _ : IDisposable{public void Dispose(){}}
        
        public class FTLoggerFactory: ILoggerFactory
        {
            public void Dispose(){}

            public ILogger CreateLogger(string categoryName) => new FTLogger(categoryName);

            public void AddProvider(ILoggerProvider provider){}
        }

        public class FTDispatcher : IDispatcherService
        {
            public void Run(Action action) => QuickInvoke.InvokeActionOnMainThread(action);
        }

        public class FTSettings : ILocalSettingsService
        {
            public Task<T> ReadSettingAsync<T>(string key)
            {
                if (ConfigManager.SelectedConfigUser == null)
                    return Task.FromResult((T) default);
                if (ConfigManager.SelectedConfigUser.FacialTrackingSettings == null)
                    ConfigManager.SelectedConfigUser.FacialTrackingSettings = new Dictionary<string, string>();
                if (!ConfigManager.SelectedConfigUser.FacialTrackingSettings.ContainsKey(key))
                    return Task.FromResult((T) default);
                return Task.FromResult(
                    JsonConvert.DeserializeObject<T>(ConfigManager.SelectedConfigUser.FacialTrackingSettings[key]));
            }

            public Task SaveSettingAsync<T>(string key, T value)
            {
                return Task.Run(() =>
                {
                    if (ConfigManager.SelectedConfigUser != null)
                    {
                        if(ConfigManager.SelectedConfigUser.FacialTrackingSettings == null)
                            ConfigManager.SelectedConfigUser.FacialTrackingSettings = new Dictionary<string, string>();
                        if (ConfigManager.SelectedConfigUser.FacialTrackingSettings.ContainsKey(key))
                            ConfigManager.SelectedConfigUser.FacialTrackingSettings[key] =
                                JsonConvert.SerializeObject(value);
                        else
                            ConfigManager.SelectedConfigUser.FacialTrackingSettings.Add(key,
                                JsonConvert.SerializeObject(value));
                    }
                });
            }
        }
    }
}