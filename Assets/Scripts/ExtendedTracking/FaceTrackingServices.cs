using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Hypernex.CCK;
using Hypernex.Configuration;
using Hypernex.Tools;
using HypernexSharp.APIObjects;
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

            public FTLogger(string c) => p = $"[FT][{c}] ";
            
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                switch (logLevel)
                {
                    case LogLevel.Information:
                        Logger.CurrentLogger.Log(p + state);
                        break;
                    case LogLevel.Warning:
                        Logger.CurrentLogger.Warn(p + state);
                        break;
                    case LogLevel.Error:
                        Logger.CurrentLogger.Error(p + state);
                        break;
                    case LogLevel.Critical:
                        Logger.CurrentLogger.Critical(new Exception(p, exception));
                        break;
                    default:
                        Logger.CurrentLogger.Debug(p + state);
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

            public Task<T> ReadSettingAsync<T>(string key, T defaultValue = default(T), bool forceLocal = false)
            {
                if (ConfigManager.SelectedConfigUser == null)
                    return Task.FromResult(defaultValue);
                if (ConfigManager.SelectedConfigUser.FacialTrackingSettings == null)
                    ConfigManager.SelectedConfigUser.FacialTrackingSettings = new Dictionary<string, string>();
                if (!ConfigManager.SelectedConfigUser.FacialTrackingSettings.ContainsKey(key))
                    return Task.FromResult(defaultValue);
                return Task.FromResult(
                    JsonConvert.DeserializeObject<T>(ConfigManager.SelectedConfigUser.FacialTrackingSettings[key]));
            }

            public Task SaveSettingAsync<T>(string key, T value, bool forceLocal = false) =>
                SaveSettingAsync(key, value);

            // Why do I have to do this? Why not just Serialize the object??
            private Dictionary<MemberInfo, SavedSettingAttribute> GetSavedSettings(object target)
            {
                Dictionary<MemberInfo, SavedSettingAttribute> members = new();
                Type targetType = target.GetType();
                foreach (FieldInfo fieldInfo in targetType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic |
                                                                     BindingFlags.Public))
                {
                    SavedSettingAttribute[] attributes = fieldInfo.GetCustomAttributes(typeof(SavedSettingAttribute))
                        .Select(x => (SavedSettingAttribute) x).ToArray();
                    if(attributes.Length <= 0) continue;
                    members.Add(fieldInfo, attributes[0]);
                }
                foreach (PropertyInfo propertyInfo in targetType.GetProperties(BindingFlags.Instance |
                                                                               BindingFlags.NonPublic |
                                                                               BindingFlags.Public))
                {
                    SavedSettingAttribute[] attributes = propertyInfo.GetCustomAttributes(typeof(SavedSettingAttribute))
                        .Select(x => (SavedSettingAttribute) x).ToArray();
                    if(attributes.Length <= 0) continue;
                    members.Add(propertyInfo, attributes[0]);
                }
                return members;
            }

            public Task Save(object target)
            {
                Dictionary<string, object> values = new();
                foreach (KeyValuePair<MemberInfo,SavedSettingAttribute> savedSetting in GetSavedSettings(target))
                {
                    object value;
                    value = savedSetting.Key is FieldInfo
                        ? ((FieldInfo) savedSetting.Key).GetValue(target)
                        : ((PropertyInfo) savedSetting.Key).GetValue(target);
                    value ??= savedSetting.Value.Default();
                    if(value == null) continue;
                    values.Add(savedSetting.Value.GetName(), value);
                }
                return SaveSettingAsync(target.GetType().FullName!.Replace(".", ""), values);
            }

            public Task Load(object target)
            {
                Dictionary<string, object> values =
                    ReadSettingAsync<Dictionary<string, object>>(target.GetType().FullName!.Replace(".", "")).Result;
                if (values == null) return Task.CompletedTask;
                foreach (KeyValuePair<MemberInfo,SavedSettingAttribute> savedSetting in GetSavedSettings(target))
                {
                    if (savedSetting.Key is FieldInfo)
                    {
                        FieldInfo fieldInfo = (FieldInfo) savedSetting.Key;
                        object value;
                        if (!values.TryGetValue(savedSetting.Value.GetName(), out value))
                            value = savedSetting.Value.Default();
                        fieldInfo.SetValue(target, Convert.ChangeType(value, fieldInfo.FieldType));
                    }
                    else if (savedSetting.Key is PropertyInfo)
                    {
                        PropertyInfo propertyInfo = (PropertyInfo) savedSetting.Key;
                        object value;
                        if (!values.TryGetValue(savedSetting.Value.GetName(), out value))
                            value = savedSetting.Value.Default();
                        propertyInfo.SetValue(target, Convert.ChangeType(value, propertyInfo.PropertyType));
                    }
                }
                return Task.CompletedTask;
            }
        }

        public class HypernexIdentity : IIdentityService
        {
            private User user;

            public HypernexIdentity(User user) => this.user = user;

            public string GetUniqueUserId() => user.Id;
        }
    }
}