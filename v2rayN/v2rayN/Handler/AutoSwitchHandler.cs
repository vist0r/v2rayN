using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Linq;
using v2rayN.Mode;

namespace v2rayN.Handler
{

    class AutoSwitchHandler
    {
        public event ProcessDelegate ProcessEvent;

        private Config _config;
        private Thread thread;

        public Func<int,int> SetServerCallback;

        private  Action autoSwitch;

        private  EventWaitHandle eventWaitHandle = new ManualResetEvent(initialState: false);

        public AutoSwitchHandler(ref Config config)
        {
            _config = config;
            autoSwitch = () =>
            {
                int lastMinIndex = 0;
                while (true)
                {
                    eventWaitHandle.WaitOne();
                    //SpeedtestHandler status = new SpeedtestHandler(ref _config);
                    long minTime = long.MaxValue;
                    int minIndex = 0;
                    for (int i = 0; i < _config.vmess.Count; i++)
                    {
                        var item = _config.vmess[i];
                        long time = Utils.Ping(item.address);
                        ShowMsg(false, FormatOut(time, "ms"));
                        if (time < minTime && time > -1)
                        {
                            minTime = time;
                            minIndex = i;
                        }
                    }
                    if (minIndex != lastMinIndex) {
                        SetServerCallback(minIndex);
                        lastMinIndex = minIndex;
                    }

                    //string result = status.RunAvailabilityCheck() + "ms";
                    Thread.Sleep(2000);
                }
            };
            thread = new Thread(()=>autoSwitch());
            thread.IsBackground = true;
        }

        public void Start()
        {
            if (thread.IsAlive)
            {
                End();
                eventWaitHandle.Set();
                return;
            }
            thread.Start();
            eventWaitHandle.Set();
        }

        public void End()
        {
            if (thread.IsAlive)
            {
                eventWaitHandle.Reset();
            }
        }

        public void Restart()
        {
            End();
            Start();
        }


        private void ShowMsg(bool updateToTrayTooltip, string msg)
        {
            ProcessEvent?.Invoke(updateToTrayTooltip, msg);
        }

        private string FormatOut(object time, string unit)
        {
            if (time.ToString().Equals("-1"))
            {
                return "Timeout";
            }
            return string.Format("{0}{1}", time, unit).PadLeft(8, ' ');
        }
    }
}
