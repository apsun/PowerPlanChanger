using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PowerPlanChanger.Properties;

namespace PowerPlanChanger
{
    public partial class Form1 : Form
    {
        private const string ConfigFilePath = "ServiceList.txt";
        private const string LogFilePath = "PowerHistory.log";
        private const int LogHeaderCharCount = 30;
        private const char LogHeaderChar = '-';

        private static readonly Icon[] ChargingIcons;
        private static readonly Icon[] DischargingIcons;
        private static readonly Icon NoBatteryIcon;
        private static readonly Icon UnknownIcon;
        private readonly NotifyIconWrapper _notifyIconWrapper;
        private readonly PowerNotificationPusher _powerNotification;
        private readonly List<Service> _selectedServices = new List<Service>();
        private readonly List<PowerPlan> _powerPlans = new List<PowerPlan>();
        private readonly StreamWriter _logWriter = new StreamWriter(LogFilePath, true);
        private Battery _prevBatteryStatus;
        private PowerPlan _prevPowerPlan;
        private bool _shiftKey;
        private bool _allowShow;

        static Form1()
        {
            ChargingIcons = new[]
            {
                BitmapToIcon(Resources.Charging10),
                BitmapToIcon(Resources.Charging20),
                BitmapToIcon(Resources.Charging30),
                BitmapToIcon(Resources.Charging40),
                BitmapToIcon(Resources.Charging50),
                BitmapToIcon(Resources.Charging60),
                BitmapToIcon(Resources.Charging70),
                BitmapToIcon(Resources.Charging80),
                BitmapToIcon(Resources.Charging90),
                BitmapToIcon(Resources.Charging100)
            };

            DischargingIcons = new[]
            {
                BitmapToIcon(Resources.Discharging10),
                BitmapToIcon(Resources.Discharging20),
                BitmapToIcon(Resources.Discharging30),
                BitmapToIcon(Resources.Discharging40),
                BitmapToIcon(Resources.Discharging50),
                BitmapToIcon(Resources.Discharging60),
                BitmapToIcon(Resources.Discharging70),
                BitmapToIcon(Resources.Discharging80),
                BitmapToIcon(Resources.Discharging90),
                BitmapToIcon(Resources.Discharging100)
            };

            NoBatteryIcon = BitmapToIcon(Resources.NoBattery);
            UnknownIcon = BitmapToIcon(Resources.Unknown);
        }
        
        public Form1()
        {
            InitializeComponent();
            Visible = false;
            DateTime dt = DateTime.Now;
            LogHeader("INITIALIZING");
            TimeLog(string.Format("Today's date: {0:0000}-{1:00}-{2:00}", dt.Year, dt.Month, dt.Day));
            _notifyIconWrapper = new NotifyIconWrapper(notificationIcon);
            _powerNotification = new PowerNotificationPusher(Handle,
                PowerNotificationPusher.PowerSourceChangedGuid,
                PowerNotificationPusher.RemainingBatteryChangedGuid,
                PowerNotificationPusher.PowerPlanChangedGuid,
                PowerNotificationPusher.DisplayStateChangedGuid);
            _powerNotification.PowerPlanChanged += _powerNotification_PowerPlanChanged;
            _powerNotification.PowerSourceChanged += _powerNotification_PowerSourceChanged;
            _powerNotification.RemainingBatteryChanged += _powerNotification_RemainingBatteryChanged;
            _powerNotification.DisplayStateChanged += _powerNotification_DisplayStateChanged;
            TimeLog("Power event notifications subscribed");
            UpdateNotificationIcon(); //Not really necessary, but just in case
            _logWriter.Flush();
        }

        private void _powerNotification_DisplayStateChanged(object sender, DisplayStateEventArgs e)
        {
            switch (e.DisplayState)
            {
                case DisplayStateEventArgs.DisplayStateType.Dimmed:
                    TimeLog("Screen dimming");
                    break;
                case DisplayStateEventArgs.DisplayStateType.Off:
                    TimeLog("Screen turned off");
                    break;
                case DisplayStateEventArgs.DisplayStateType.On:
                    TimeLog("Screen turned on");
                    break;
            }
        }

        private void _powerNotification_RemainingBatteryChanged(object sender, RemainingBatteryEventArgs e)
        {
            TimeLog("Battery remaining changed to " + e.RemainingBattery + "%");
        }

        private void _powerNotification_PowerSourceChanged(object sender, PowerSourceEventArgs e)
        {
            TimeLog("Power source changed to " + e.PowerSource);
        }

        private void _powerNotification_PowerPlanChanged(object sender, PowerPlanEventArgs e)
        {
            PowerPlan pp = PowerPlan.FromGuid(e.PowerPlanGuid);
            TimeLog("Power plan changed to " + pp.Name + " [" + pp.Guid + "]");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (!File.Exists(ConfigFilePath)) return;

            if (LoadConfig()) SaveConfig();
            SortSelectedServiceList();
            PopulateServiceListBox();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            HideForm();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Shift)
            {
                startButton.Text = "Start all";
                stopButton.Text = "Stop all";
                _shiftKey = true;
            }
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (!e.Shift)
            {
                startButton.Text = "Start";
                stopButton.Text = "Stop";
                _shiftKey = false;
            }
        }

        private void Form1_Activated(object sender, EventArgs e)
        {
            if ((ModifierKeys & Keys.ShiftKey) != 0)
            {
                startButton.Text = "Start all";
                stopButton.Text = "Stop all";
                _shiftKey = true;
            }
            else
            {
                startButton.Text = "Start";
                stopButton.Text = "Stop";
                _shiftKey = false;
            }

            UpdateServiceListBox();
        }

        private void Form1_Deactivate(object sender, EventArgs e)
        {
            startButton.Text = "Start";
            stopButton.Text = "Stop";
            _shiftKey = false;
        }

        private void addButton_Click(object sender, EventArgs e)
        {
            using (var addServiceDialog = new AddServiceDialog())
            {
                if (addServiceDialog.ShowDialog() == DialogResult.Cancel) return;
                AddServices(addServiceDialog.SelectedServices);
            }
        }

        private void removeButton_Click(object sender, EventArgs e)
        {
            RemoveServicesAt(serviceListBox.SelectedIndices.Cast<int>());
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            if (_shiftKey)
            {
                foreach (Service svc in _selectedServices)
                {
                    svc.Start();
                }
            }
            else
            {
                foreach (int index in serviceListBox.SelectedIndices)
                {
                    _selectedServices[index].Start();
                }
            }

            UpdateServiceListBox();
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            if (_shiftKey)
            {
                foreach (Service svc in _selectedServices)
                {
                    svc.Stop();
                }
            }
            else
            {
                foreach (int selIndex in serviceListBox.SelectedIndices)
                {
                    _selectedServices[selIndex].Stop();
                }
            }

            UpdateServiceListBox();
        }

        private void refreshButton_Click(object sender, EventArgs e)
        {
            UpdateServiceListBox();
        }

        private void notificationIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _allowShow = true;
                ToggleVisible();
            }
        }

        private void rightClickMenu_Popup(object sender, EventArgs e)
        {
            _powerPlans.Clear();
            _powerPlans.AddRange(PowerPlan.GetPowerPlans());

            var menuItems = rightClickMenu.MenuItems;
            while (menuItems.Count > 0)
            {
                MenuItem menuItem = menuItems[menuItems.Count - 1];
                menuItem.Click -= menuItem_Click;
                menuItem.Dispose();
            }

            foreach (PowerPlan plan in _powerPlans)
            {
                var menuItem = new DataMenuItem<PowerPlan>(plan, pp => pp.Name);
                rightClickMenu.MenuItems.Add(menuItem);
                menuItem.Click += menuItem_Click;
            }
        }

        private void menuItem_Click(object sender, EventArgs e)
        {
            var menuItem = (DataMenuItem<PowerPlan>)sender;
            PowerPlan pp = menuItem.Data;
            if (pp == _prevPowerPlan) return;
            pp.SetActive();
            LogHeader("POWER PLAN CHANGED");
            TimeLog("Power plan changed to " + pp.Name + " [" + pp.Guid + "]");
            UpdateNotificationIcon();
            _logWriter.Flush();
        }

        private void UpdateNotificationIcon()
        {
            // ReSharper disable PossibleInvalidOperationException
            Battery batteryInfo = Battery.GetInformation();
            PowerPlan powerPlanInfo = PowerPlan.GetActivePowerPlan();
            if (batteryInfo == _prevBatteryStatus && powerPlanInfo == _prevPowerPlan)
            {
                //TimeLog("Power status did not change");
                return;
            }
            _prevBatteryStatus = batteryInfo;
            _prevPowerPlan = powerPlanInfo;

            BatteryStatus status = batteryInfo.PowerStatus;
            //TimeLog("Battery status: " + status);
            //TimeLog("Current power plan: " + powerPlanInfo.Name);
            TimeLog("Notification icon refreshed");

            Icon icon;
            switch (status)
            {
                case BatteryStatus.Unknown:
                    icon = UnknownIcon;
                    break;
                case BatteryStatus.NoBattery:
                    icon = NoBatteryIcon;
                    break;
                default:
                    Icon[] icons = batteryInfo.IsPluggedIn.Value ? ChargingIcons : DischargingIcons;
                    icon = icons[(batteryInfo.RemainingCharge.Value - 1) / 10];
                    break;
            }

            string chargeText;
            switch (status)
            {
                case BatteryStatus.Charging:
                case BatteryStatus.Discharging:
                case BatteryStatus.NotCharging:
                    chargeText = " (" + batteryInfo.RemainingCharge.Value + "%)";
                    break;
                default:
                    chargeText = string.Empty;
                    break;
            }

            string statusText;
            switch (status)
            {
                case BatteryStatus.FullyCharged:
                    statusText = "Fully charged";
                    break;
                case BatteryStatus.Charging:
                    statusText = "Charging";
                    break;
                case BatteryStatus.NotCharging:
                    statusText = "Plugged in, not charging";
                    break;
                case BatteryStatus.Discharging:
                    statusText = "Discharging";
                    break;
                case BatteryStatus.NoBattery:
                    statusText = "No battery";
                    break;
                default: //BatteryStatus.Unknown
                    statusText = "Unknown";
                    break;
            }

            string activePlan = powerPlanInfo.Name;
            string newCaption = "Battery status: " + statusText + chargeText + "\n" +
                                "Active power plan: " + activePlan;
            _notifyIconWrapper.Text = newCaption;
            _notifyIconWrapper.Icon = icon;
        }

        private void HideForm()
        {
            serviceListBox.SelectedIndices.Clear();
            Hide();
        }

        private void ShowForm()
        {
            WindowState = FormWindowState.Minimized;
            Show();
            WindowState = FormWindowState.Normal;
        }

        private void ToggleVisible()
        {
            if (!Visible) ShowForm(); else HideForm();
        }

        private void SortSelectedServiceList()
        {
            _selectedServices.Sort((a, b) =>
                string.Compare(a.ServiceName, b.ServiceName, StringComparison.OrdinalIgnoreCase));
        }

        private void PopulateServiceListBox()
        {
            serviceListBox.Items.Clear();
            serviceListBox.BeginUpdate();

            bool changed = false;

            for (int i = 0; i < _selectedServices.Count; i++)
            {
                Service svc = _selectedServices[i];

                if (!ServiceExists(svc))
                {
                    _selectedServices.RemoveAt(i);
                    serviceListBox.Items.RemoveAt(i);
                    --i;
                    changed = true;
                    continue;
                }

                string status = svc.IsRunning ? "[Y] " : "[N] ";

                serviceListBox.Items.Add(status + svc.ServiceName);
            }

            serviceListBox.EndUpdate();
            if (changed) SaveConfig();
        }

        private void UpdateServiceListBox()
        {
            bool changed = false;
            var selIndicies = serviceListBox.SelectedIndices;

            for (int i = 0; i < _selectedServices.Count; i++)
            {
                Service svc = _selectedServices[i];
                
                if (!ServiceExists(svc))
                {
                    _selectedServices.RemoveAt(i);
                    serviceListBox.Items.RemoveAt(i);
                    --i;
                    changed = true;
                    continue;
                }

                bool isSelected = selIndicies.Contains(i);
                string status = svc.IsRunning ? "[Y] " : "[N] ";

                serviceListBox.Items[i] = status + svc.ServiceName;
                if (isSelected) serviceListBox.SelectedIndices.Add(i);
            }

            if (changed) SaveConfig();
        }

        private bool LoadConfig()
        {
            _selectedServices.Clear();
            var set = new HashSet<string>(Service.GetServices().Select(s => s.ServiceName));
            bool deleted = false;
            foreach (string line in File.ReadAllLines(ConfigFilePath))
            {
                if (!set.Contains(line))
                {
                    deleted = true;
                    continue;
                }

                _selectedServices.Add(Service.FromName(line));
            }
            return deleted;
        }

        private void SaveConfig()
        {
            using (var file = new StreamWriter(ConfigFilePath, false))
            {
                foreach (Service svc in _selectedServices)
                {
                    file.WriteLine(svc.ServiceName);
                }
            }
        }

        private void Log(string message)
        {
            Debug.WriteLine(message);
            _logWriter.WriteLine(message);
        }

        private void TimeLog(string message)
        {
            DateTime dt = DateTime.Now;
            Log(string.Format("[{0:00}:{1:00}:{2:00}] {3}", dt.Hour, dt.Minute, dt.Second, message));
        }

        private void LogHeader(string message)
        {
            int pad = (LogHeaderCharCount - message.Length) / 2;
            var sb = new StringBuilder(LogHeaderCharCount);
            sb.Append(LogHeaderChar, pad);
            sb.Append(message);
            sb.Append(LogHeaderChar, LogHeaderCharCount - message.Length - pad);
            Log(sb.ToString());
        }

        private void AddServices(IEnumerable<Service> services)
        {
            foreach (Service svc in services.Where(svc => !_selectedServices.Contains(svc)))
            {
                _selectedServices.Add(svc);
            }

            SortSelectedServiceList();
            PopulateServiceListBox();
            SaveConfig();
        }

        private void RemoveServicesAt(IEnumerable<int> indices)
        {
            foreach (int item in indices.OrderByDescending(i => i))
            {
                _selectedServices.RemoveAt(item);
            }

            SortSelectedServiceList();
            PopulateServiceListBox();
            SaveConfig();
        }

        private static Icon BitmapToIcon(Bitmap bmp)
        {
            return Icon.FromHandle(bmp.GetHicon());
        }

        private static bool ServiceExists(Service svc)
        {
            try
            {
                // ReSharper disable once UnusedVariable
                bool temp = svc.IsRunning;
            }
            catch (InvalidOperationException)
            {
                return false;
            }

            return true;
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(_allowShow && value);
        }

        protected override void WndProc(ref Message m)
        {
            while (m.Msg == 536)
            {
                int wParam = m.WParam.ToInt32();
                if (wParam != 10 && wParam != 32787) break;
                LogHeader("POWER EVENT RECEIVED");
                if (wParam == 10)
                    TimeLog("General event processed");
                else if (wParam == 32787)
                    _powerNotification.ProcessMessage(m);
                UpdateNotificationIcon();
                _logWriter.Flush();
                m.Result = (IntPtr)1;
                break;
            }
            
            base.WndProc(ref m);
        }
    }
}
