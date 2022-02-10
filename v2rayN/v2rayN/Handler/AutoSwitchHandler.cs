using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Linq;
using v2rayN.Mode;
using RestSharp;
using System.Threading.Tasks;
using Microsoft.Graph;
using Azure.Identity;

namespace v2rayN.Handler
{

    class AutoSwitchHandler
    {
        public event ProcessDelegate ProcessEvent;

        private Config _config;
        private Thread thread;

        private static GraphServiceClient graphClient;

        public Func<int,int> SetServerCallback;

        private  Action autoSwitch;

        private  EventWaitHandle eventWaitHandle = new ManualResetEvent(initialState: false);

        public AutoSwitchHandler(ref Config config)
        {
            _config = config;
            autoSwitch = async () =>
            {
                int lastMinIndex = 0;
                int latencyInterval = 1000 * 10 * 60;
                while (true)
                {
                    eventWaitHandle.WaitOne();
                    SpeedtestHandler status = new SpeedtestHandler(ref _config);
                    int result = status.RunAvailabilityCheck();
                    if (result != -1 && result < 1000) {
                        Thread.Sleep(latencyInterval);
                        continue;
                    }
                    long minTime = long.MaxValue;
                    int minIndex = 0;
                    List<long> timeList = new List<long>();
                    for (int i = 0; i < _config.vmess.Count; i++)
                    {
                        var item = _config.vmess[i];
                        long time = Utils.Ping(item.address);
                        timeList.Add(time);
                        if (time < minTime && time > -1)
                        {
                            minTime = time;
                            minIndex = i;
                        }
                    }
                    if (minIndex != lastMinIndex) {
                        SetServerCallback(minIndex);
                        lastMinIndex = minIndex;
                        string vmessFromInfo = _config.vmess[lastMinIndex].getSummary();
                        string vmessToInfo = _config.vmess[minIndex].getSummary();
                        string allClientInfo = String.Empty;
                        for (int i = 0; i < _config.vmess.Count; i++) {
                            allClientInfo += _config.vmess[i].getSummary() + String.Format(" Latency {0} ms", timeList[i]);
                            allClientInfo += "\n";
                        }
                        await SendReport(lastMinIndex, minIndex, String.Format("Change {0} \n to \n {1} \n Current latency: {2} ms \n  {3}", vmessFromInfo, vmessToInfo, minTime, allClientInfo));
                    }
                }
            };
            thread = new Thread(()=>autoSwitch());
            thread.IsBackground = true;
            initGraphClient();
        }

        public void Start()
        {
            var state = thread.ThreadState;
            if ((state & System.Threading.ThreadState.Unstarted) == System.Threading.ThreadState.Unstarted)
            {
                thread.Start();
                eventWaitHandle.Set();
                return;
            }

            if ((state & System.Threading.ThreadState.Background) == System.Threading.ThreadState.Background)
            {
                End();
                eventWaitHandle.Set();
                return;
            }

            if ((state & System.Threading.ThreadState.Stopped) == System.Threading.ThreadState.Stopped)
            {
                eventWaitHandle.Set();
                return;
            }

        }

        public void End()
        {
            eventWaitHandle.Reset();
        }

        public void Restart()
        {
            End();
            Start();
            ShowMsg(false, "Auto switcher started ...");
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

        private void initGraphClient() {
            var scopes = new[] { "https://graph.microsoft.com/.default" };
            var tenantId = "c6266088-54af-4bb7-8b8d-772cd06e63f8";

            var clientId = "14e84cd3-d7a1-458e-9401-ba35d7a74056";
            var clientSecret = "Bl25ot-YGW7re~Bz4XV2zT6.0l-h3gAVX-";

            var options = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };

            var clientSecretCredential = new ClientSecretCredential(
                tenantId, clientId, clientSecret, options);

            graphClient = new GraphServiceClient(clientSecretCredential, scopes);
        }

        private async Task SendReport(int from, int to, string msg) {
            var message = new Message
            {
                Subject = "Report from worker " + System.Environment.MachineName,
                Body = new ItemBody
                {
                    ContentType = BodyType.Text,
                    Content = msg
                },
                ToRecipients = new List<Recipient>()
                {
                    new Recipient
                    {
                        EmailAddress = new EmailAddress
                        {
                            Address = "402692034@qq.com"
                        }
                    }
                }
            };

            await graphClient.Users["36d5462d-441a-4357-b33e-2c8b095e0308"]
                .SendMail(message, null)
                .Request()
                .PostAsync();
        }
    }
}
