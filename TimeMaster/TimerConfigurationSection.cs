using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeMaster
{
    public class TimerConfigurationSection : ConfigurationSection
    {
        [ConfigurationProperty("", IsDefaultCollection = true)]
        public TimerCollection Timers
        {
            get { return (TimerCollection)this[""]; }
        }
    }

    public class TimerCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new TimerElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((TimerElement)element).Name;
        }

        public TimerElement this[int index]
        {
            get { return (TimerElement)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                    BaseRemoveAt(index);
                BaseAdd(index, value);
            }
        }

        public new TimerElement this[string name] => (TimerElement)BaseGet(name);

        public int IndexOf(TimerElement element)
        {
            return BaseIndexOf(element);
        }

        public void Add(TimerElement element)
        {
            BaseAdd(element);
        }

        public void Remove(TimerElement element)
        {
            if (BaseIndexOf(element) >= 0)
                BaseRemove(element.Name);
        }

        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);
        }

        public void Remove(string name)
        {
            BaseRemove(name);
        }

        public void Clear()
        {
            BaseClear();
        }

    }


    public class TimerElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true, IsKey = true)]
        public string Name
        {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }

        [ConfigurationProperty("time", IsRequired = true)]
        public string Time
        {
            get { return (string)this["time"]; }
            set { this["time"] = value; }
        }

        [ConfigurationProperty("isActive", DefaultValue = true)]
        public bool IsActive
        {
            get { return (bool)this["isActive"]; }
            set { this["isActive"] = value; }
        }

        [ConfigurationProperty("autoCloseTip", DefaultValue = true)]
        public bool AutoCloseTip
        {
            get { return (bool)this["autoCloseTip"]; }
            set { this["autoCloseTip"] = value; }
        }

        [ConfigurationProperty("tipLine1", IsRequired = true)]
        public string TipLine1
        {
            get { return (string)this["tipLine1"]; }
            set { this["tipLine1"] = value; }
        }

        [ConfigurationProperty("tipLine2", IsRequired = false, DefaultValue = "")]
        public string TipLine2
        {
            get { return (string)this["tipLine2"]; }
            set { this["tipLine2"] = value; }
        }

        public int CountdownSeconds => 60;

        string countdownTime = string.Empty;
        public string GetCountdownTime()
        {
            if (string.IsNullOrEmpty(countdownTime))
            {
                if (!TimeSpan.TryParse(Time, out var triggerTime))
                {
                    throw new FormatException($"时间格式无效: {Time}");
                }
                // 减去60秒
                var cTime = triggerTime.Subtract(TimeSpan.FromSeconds(this.CountdownSeconds));
                // 处理跨天情况（结果为负值时）
                if (cTime.TotalSeconds < 0)
                {
                    cTime = cTime.Add(TimeSpan.FromDays(1));
                }
                countdownTime = cTime.ToString(@"hh\:mm\:ss");
            }
            return countdownTime;
        }

        public override string ToString()
        {
            string status = IsActive ? "√ 启用" : "× 禁用";
            string tip2 = string.IsNullOrEmpty(TipLine2) ? "无" : TipLine2;
            return $"定时任务 [{Name}]\n" +
                   $"├─ 触发时间: {Time}\n" +
                   $"├─ 状态: {status}\n" +
                   $"├─ 主要提示: {TipLine1}\n" +
                   $"└─ 次要提示: {tip2}";
        }
    }

}
