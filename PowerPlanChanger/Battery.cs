using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace PowerPlanChanger
{
    /// <summary>
    /// Combines the System.Windows.Forms.PowerLineStatus and 
    /// System.Windows.Forms.BatteryChargeStatus enums.
    /// </summary>
    public enum BatteryStatus
    {
        /// <summary>
        /// Indicates that the battery is plugged in and fully charged.
        /// </summary>
        FullyCharged,
        /// <summary>
        /// Indicates that the computer is plugged in and charging.
        /// </summary>
        Charging,
        /// <summary>
        /// Indicates that the computer is plugged in but not charging.
        /// </summary>
        NotCharging,
        /// <summary>
        /// Indicates that the computer is is running off battery power.
        /// </summary>
        Discharging,
        /// <summary>
        /// Indicates that the computer is plugged into an AC power source 
        /// and does not have a battery installed.
        /// </summary>
        NoBattery,
        /// <summary>
        /// Indicates that the status of the battery is unknown.
        /// </summary>
        Unknown
    }

    /// <summary>
    /// Contains utilities to query the battery status.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Battery : IEquatable<Battery>
    {
        #region P/Invoke

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GetSystemPowerStatus(out Battery systemPowerStatus);

// ReSharper disable InconsistentNaming
#pragma warning disable 649
        private readonly byte ACLineStatus;
        private readonly byte BatteryFlag;
        private readonly byte BatteryLifePercent;
#pragma warning disable 169
        private readonly byte Reserved1;
#pragma warning restore 169
        private readonly int BatteryLifeTime;
        private readonly int BatteryFullLifeTime;
#pragma warning restore 649
// ReSharper restore InconsistentNaming

        #endregion

        /// <summary>
        /// Gets information about the current state of the battery.
        /// </summary>
        public static Battery GetInformation()
        {
            Battery b;
            if (!GetSystemPowerStatus(out b))
                throw new Win32Exception();
            return b;
        }

        /// <summary>
        /// Gets the battery's charge in the range [0, 100], 
        /// or null if the battery charge is unknown.
        /// </summary>
        public int? RemainingCharge
        {
            get
            {
                byte batteryLifePercent = BatteryLifePercent;
                if (batteryLifePercent == 255) return null;
                return batteryLifePercent;
            }
        }

        /// <summary>
        /// Gets whether there is a battery installed on the computer, 
        /// or null if the battery status is unknown.
        /// </summary>
        public bool? BatteryExists
        {
            get
            {
                byte batteryFlag = BatteryFlag;
                if (batteryFlag == 255) return null;
                return (batteryFlag & 128) == 0;
            }
        }

        /// <summary>
        /// Gets whether the computer is connected to an AC power source, 
        /// or null if the connectivity status is unknown.
        /// </summary>
        public bool? IsPluggedIn
        {
            get
            {
                byte acLineStatus = ACLineStatus;
                if (acLineStatus == 255) return null;
                return acLineStatus == 1;
            }
        }

        /// <summary>
        /// Gets whether the battery is charging, or null if the battery 
        /// status is unknown. This is NOT synonymous with IsPluggedIn; there 
        /// are times where the charger is connected but the battery is not charging.
        /// </summary>
        public bool? IsCharging
        {
            get
            {
                byte batteryFlag = BatteryFlag;
                if (batteryFlag == 255) return null;
                return (batteryFlag & 8) != 0;
            }
        }

        /// <summary>
        /// Gets the remaining battery life, or null if the battery status 
        /// is unknown.
        /// </summary>
        public TimeSpan? RemainingBatteryLifeTime
        {
            get
            {
                int batteryLifeTime = BatteryLifeTime;
                if (batteryLifeTime == -1) return null;
                return TimeSpan.FromSeconds(batteryLifeTime);
            }
        }

        /// <summary>
        /// Gets the battery life when fully charged, or null if the battery 
        /// status is unknown.
        /// </summary>
        public TimeSpan? FullBatteryLifeTime
        {
            get
            {
                int batteryFullLifeTime = BatteryFullLifeTime;
                if (batteryFullLifeTime == -1) return null;
                return TimeSpan.FromSeconds(batteryFullLifeTime);
            }
        }

        /// <summary>
        /// Gets the combined status of the battery and charger.
        /// </summary>
        public BatteryStatus PowerStatus
        {
            get
            {
                if (BatteryFlag == 255)
                    return BatteryStatus.Unknown; //Unknown battery status
                if (ACLineStatus == 255)
                    return BatteryStatus.Unknown; //Unknown AC status
                if (ACLineStatus == 0)
                    if (BatteryLifePercent == 255)
                        return BatteryStatus.Unknown; //Not plugged in, unknown battery status
                    else
                        return BatteryStatus.Discharging; //Must be discharging
                if ((BatteryFlag & 128) != 0)
                    return BatteryStatus.NoBattery; //No battery
                if (BatteryLifePercent == 100)
                    return BatteryStatus.FullyCharged; //Fully charged
                if ((BatteryFlag & 8) != 0)
                    return BatteryStatus.Charging; //Plugged in, must be charging
                return BatteryStatus.NotCharging; //Plugged in, but not charging
            }
        }

        public bool Equals(Battery other)
        {
            return ACLineStatus == other.ACLineStatus && 
                   BatteryFlag == other.BatteryFlag && 
                   BatteryLifePercent == other.BatteryLifePercent && 
                   BatteryLifeTime == other.BatteryLifeTime && 
                   BatteryFullLifeTime == other.BatteryFullLifeTime;
        }
        
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Battery && Equals((Battery)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = ACLineStatus;
                hashCode = (hashCode * 397) ^ BatteryFlag;
                hashCode = (hashCode * 397) ^ BatteryLifePercent;
                hashCode = (hashCode * 397) ^ BatteryLifeTime;
                hashCode = (hashCode * 397) ^ BatteryFullLifeTime;
                return hashCode;
            }
        }

        public static bool operator ==(Battery a, Battery b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Battery a, Battery b)
        {
            return !a.Equals(b);
        }
    }
}