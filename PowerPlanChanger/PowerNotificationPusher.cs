using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PowerPlanChanger
{
    public abstract class PowerEventArgs : EventArgs
    {

    }

    public class PowerSourceEventArgs : PowerEventArgs
    {
        public enum SystemPowerCondition
        {
            AC,
            Battery,
            UPS,
            Other
        }

        private readonly SystemPowerCondition _powerSource;
        public SystemPowerCondition PowerSource
        {
            get { return _powerSource; }
        }

        internal PowerSourceEventArgs(int powerSource)
        {
            _powerSource = (SystemPowerCondition)powerSource;
        }
    }

    public class RemainingBatteryEventArgs : PowerEventArgs
    {
        private readonly int _remainingBattery;
        public int RemainingBattery
        {
            get { return _remainingBattery; }
        }

        internal RemainingBatteryEventArgs(int remainingBattery)
        {
            _remainingBattery = remainingBattery;
        }
    }

    public class DisplayStateEventArgs : PowerEventArgs
    {
        public enum DisplayStateType
        {
            Off,
            On,
            Dimmed
        }

        private readonly DisplayStateType _displayState;
        public DisplayStateType DisplayState
        {
            get { return _displayState; }
        }

        internal DisplayStateEventArgs(int displayState)
        {
            _displayState = (DisplayStateType)displayState;
        }
    }

    public class PowerPlanEventArgs : PowerEventArgs
    {
        private readonly Guid _powerPlanGuid;
        public Guid PowerPlanGuid
        {
            get { return _powerPlanGuid; }
        }

        internal PowerPlanEventArgs(Guid guid)
        {
            _powerPlanGuid = guid;
        }
    }

    public class PowerNotificationPusher : IDisposable
    {
        public event EventHandler<PowerSourceEventArgs> PowerSourceChanged;
        public event EventHandler<RemainingBatteryEventArgs> RemainingBatteryChanged;
        public event EventHandler<DisplayStateEventArgs> DisplayStateChanged;
        public event EventHandler<PowerPlanEventArgs> PowerPlanChanged;

        #region P/Invoke

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr RegisterPowerSettingNotification(IntPtr hRecipient, ref Guid powerSettingGuid, int flags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterPowerSettingNotification(IntPtr handle);

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct PowerBroadcastSetting
        {
            //http://msdn.microsoft.com/en-us/library/windows/desktop/aa372723%28v=vs.85%29.aspx
            public readonly Guid PowerSetting;
            public readonly int DataLength;
        }

        #endregion

        #region Constants

        public static readonly Guid PowerSourceChangedGuid;
        public static readonly Guid RemainingBatteryChangedGuid;
        public static readonly Guid DisplayStateChangedGuid;
        public static readonly Guid PowerPlanChangedGuid;

        #endregion

        static PowerNotificationPusher()
        {
            PowerSourceChangedGuid = new Guid("5d3e9a59-e9D5-4b00-a6bd-ff34ff516548");
            RemainingBatteryChangedGuid = new Guid("a7ad8041-b45a-4cae-87a3-eecbb468a9e1");
            DisplayStateChangedGuid = new Guid("6fe69556-704a-47a0-8f24-c28d936fda47");
            PowerPlanChangedGuid = new Guid("245d8541-3943-4422-b025-13a784f679b7");
        }

        private readonly IntPtr[] _handles;

        public PowerNotificationPusher(IntPtr hRecipient, params Guid[] guids)
        {
            _handles = new IntPtr[guids.Length];
            for (int i = 0; i < guids.Length; ++i)
                _handles[i] = RegisterNotification(hRecipient, guids[i]);
        }

        public void ProcessMessage(Message m)
        {
            var pbs = (PowerBroadcastSetting)Marshal.PtrToStructure(m.LParam, typeof(PowerBroadcastSetting));
            if (pbs.PowerSetting == PowerSourceChangedGuid)
            {
                AssertSize(pbs, typeof(int));
                OnPowerSourceChanged(new PowerSourceEventArgs(DataToDword(m, pbs)));
            }
            else if (pbs.PowerSetting == RemainingBatteryChangedGuid)
            {
                AssertSize(pbs, typeof(int));
                OnRemainingBatteryChanged(new RemainingBatteryEventArgs(DataToDword(m, pbs)));
            }
            else if (pbs.PowerSetting == DisplayStateChangedGuid)
            {
                AssertSize(pbs, typeof(int));
                OnDisplayStateChanged(new DisplayStateEventArgs(DataToDword(m, pbs)));
            }
            else if (pbs.PowerSetting == PowerPlanChangedGuid)
            {
                AssertSize(pbs, typeof(Guid));
                OnPowerPlanChanged(new PowerPlanEventArgs(DataToGuid(m, pbs)));
            }
        }

        private static void AssertSize(PowerBroadcastSetting pbs, Type t)
        {
            if (pbs.DataLength != Marshal.SizeOf(t))
                throw new InvalidCastException();
        }

        private static int DataToDword(Message m, PowerBroadcastSetting pbs)
        {
            var pData = new IntPtr(m.LParam.ToInt32() + Marshal.SizeOf(pbs));
            return (int)Marshal.PtrToStructure(pData, typeof(int));
        }

        private static Guid DataToGuid(Message m, PowerBroadcastSetting pbs)
        {
            var pData = new IntPtr(m.LParam.ToInt32() + Marshal.SizeOf(pbs));
            return (Guid)Marshal.PtrToStructure(pData, typeof(Guid));
        }

        private static IntPtr RegisterNotification(IntPtr hRecipient, Guid powerSettingGuid)
        {
            IntPtr handle = RegisterPowerSettingNotification(hRecipient, ref powerSettingGuid, 0);
            if (handle == IntPtr.Zero) throw new Win32Exception();
            return handle;
        }

        private static void UnregisterNotification(IntPtr handle)
        {
            if (!UnregisterPowerSettingNotification(handle))
                throw new Win32Exception();
        }

        protected virtual void OnPowerSourceChanged(PowerSourceEventArgs e)
        {
            EventHandler<PowerSourceEventArgs> handler = PowerSourceChanged;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnRemainingBatteryChanged(RemainingBatteryEventArgs e)
        {
            EventHandler<RemainingBatteryEventArgs> handler = RemainingBatteryChanged;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnDisplayStateChanged(DisplayStateEventArgs e)
        {
            EventHandler<DisplayStateEventArgs> handler = DisplayStateChanged;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnPowerPlanChanged(PowerPlanEventArgs e)
        {
            EventHandler<PowerPlanEventArgs> handler = PowerPlanChanged;
            if (handler != null) handler(this, e);
        }

        protected void Dispose(bool disposing)
        {
            for (int i = 0; i < _handles.Length; i++)
            {
                if (_handles[i] == IntPtr.Zero) continue;
                try
                {
                    UnregisterNotification(_handles[i]);
                    _handles[i] = IntPtr.Zero;
                }
                catch (Win32Exception)
                {
                    if (disposing) throw;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        ~PowerNotificationPusher()
        {
            Dispose(false);
            GC.SuppressFinalize(this);
        }
    }
}
