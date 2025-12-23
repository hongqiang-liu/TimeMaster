using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeMaster
{
    public static class TimerConfigHelper
    {
        private static TimerConfigurationSection GetTimerSection()
        {
            return ConfigurationManager.GetSection("timers") as TimerConfigurationSection;
        }

        public static TimerCollection GetTimers()
        {
            return GetTimerSection()?.Timers;
        }

        public static TimerElement GetTimer(string name)
        {
            return GetTimers()?[name];
        }

        public static void AddTimer(TimerElement timer)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var timerSection = (TimerConfigurationSection)config.GetSection("timers");
            timerSection.Timers.Add(timer);
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("timers");
        }

        public static void UpdateTimer(TimerElement timer)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var timerSection = (TimerConfigurationSection)config.GetSection("timers");
            var existingTimer = timerSection.Timers[timer.Name];
            if (existingTimer != null)
            {
                timerSection.Timers.Remove(existingTimer);
                timerSection.Timers.Add(timer);
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("timers");
            }
        }

        public static void RemoveTimer(string name)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var timerSection = (TimerConfigurationSection)config.GetSection("timers");
            var timer = timerSection.Timers[name];
            if (timer != null)
            {
                timerSection.Timers.Remove(timer);
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("timers");
            }
        }
    }
}
