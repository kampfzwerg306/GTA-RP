﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace GTA_RP.Misc
{
    public delegate void GTRPTimerDelegate(GTRPTimer timer);
    public class GTRPTimer
    {
        private Timer timer = new Timer();
        private GTRPTimerDelegate delegateMethod;
        private bool repeat = false;

        private void TimerElapsedMethod(System.Object source, ElapsedEventArgs args)
        {
            this.delegateMethod.Invoke(this);
            if (this.repeat) this.timer.Enabled = true;
        }
        public GTRPTimer(GTRPTimerDelegate method, int time, bool repeat = false)
        {
            this.repeat = repeat;
            this.delegateMethod = method;
            this.timer.Interval = time;
            timer.Elapsed += this.TimerElapsedMethod;
        }

        public void Start()
        {
            this.timer.Start();
        }

        public void Stop()
        {
            this.timer.Enabled = false;
            this.timer.Stop();
        }
    }
}
