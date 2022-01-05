using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using v2rayN.Mode;

namespace v2rayN.Handler
{

    class AutoSwitchHandler
    {
        public event ProcessDelegate ProcessEvent;

        private Config _config;
        private Thread thread;

        public AutoSwitchHandler(ref Config config)
        {
            _config = config;
            thread = new Thread(() =>
            {
                int x = 0;
                while (true)
                {
                    SpeedtestHandler status = new SpeedtestHandler(ref _config);
                    string result = status.RunAvailabilityCheck() + "ms";
                    ShowMsg(false, result);
                    x++;
                    Thread.Sleep(1000);
                }
            });
            thread.IsBackground = true;
        }

        public void Start()
        {
            thread.Start();
        }

        public void End()
        {
            if (thread.IsAlive)
            {
                thread.Abort();
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
    }
}
