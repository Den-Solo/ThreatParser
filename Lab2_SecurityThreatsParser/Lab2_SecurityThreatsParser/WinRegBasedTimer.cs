using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using Microsoft.Win32;

namespace Lab2_SecurityThreatsParser
{
    class WinRegBasedTimer
    {
        public delegate void TimeEventHandler();
        public TimeEventHandler _TimeHasCome;
        public int _sleepTime = 30_000; //millisec
        public TimeSpan _TimeInterval { get; private set; }
        private const string _appName = "ISThreatParser";
        private const string _regValueName = "LastAutoUpdate";
        private Thread _th;
        public bool isTimeToStop = false;
        public WinRegBasedTimer(TimeSpan time, TimeEventHandler handler, Window parent)
        {
            _TimeInterval = time;
            _TimeHasCome += handler;

            parent.Closed += Close_Handler;

            RegistryKey key = Registry.CurrentUser.OpenSubKey("Software", true);
            key = key.OpenSubKey(_appName);
            if (key == null) //create new
            {
                SetStartTime();
            }
            key.Dispose();
            _th = new Thread(CheckTime);
            _th.Start();
        }
        private void CheckTime()
        {
            while (true)
            {
                DateTime startMoment = GetStartTime();
                if (DateTime.Now >= startMoment + _TimeInterval)
                {
                    _TimeHasCome?.Invoke();
                    SetStartTime();
                }
                Thread.Sleep(_sleepTime);
            }
        }
        private DateTime GetStartTime()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software", true))
            {
                using (var subkey = key.OpenSubKey(_appName))
                {
                    return Convert.ToDateTime(subkey.GetValue(_regValueName).ToString());
                }
            }
        }
        private void SetStartTime()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software", true))
            {
                using (var subkey = key.CreateSubKey(_appName))
                {
                    subkey.SetValue(_regValueName, DateTime.Now.ToString());
                }
            }
        }
        private void Close_Handler(object sender, EventArgs e)
        {
            _th.Abort();
        }
    }
}
