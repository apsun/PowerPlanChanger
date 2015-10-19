using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;

namespace PowerPlanChanger
{
    /// <summary>
    /// Represents a power plan on the computer.
    /// </summary>
    public struct PowerPlan : IEquatable<PowerPlan>
    {
        #region P/Invoke

        [DllImport("powrprof.dll")]
        private static extern uint PowerSetActiveScheme(IntPtr userPowerKey, ref Guid activePolicyGuid);

        [DllImport("powrprof.dll")]
        private static extern uint PowerGetActiveScheme(IntPtr userPowerKey, out IntPtr activePolicyGuid);

        [DllImport("powrprof.dll")]
        private static extern uint PowerReadFriendlyName(IntPtr rootPowerKey, ref Guid schemeGuid, IntPtr subGroupOfPowerSettingGuid, IntPtr powerSettingGuid, IntPtr buffer, ref uint bufferSize);

        [DllImport("powrprof.dll")]
        private static extern uint PowerEnumerate(IntPtr rootPowerKey, IntPtr schemeGuid, IntPtr subGroupOfPowerSettingGuid, AccessFlags acessFlags, uint index, ref Guid buffer, ref UInt32 bufferSize);
        
        [DllImport("powrprof.dll")]
        private static extern uint PowerReadDescription(IntPtr rootPowerKey, ref Guid schemeGuid, IntPtr subGroupOfPowerSettingGuid, IntPtr powerSettingGuid, IntPtr buffer, ref uint bufferSize);

        private enum AccessFlags : uint
        {
            Scheme = 16,
            //Subgroup = 17,
            //IndividualSetting = 18
        }

        #endregion

        #region Constants

        public static readonly PowerPlan PowerSaver;
        public static readonly PowerPlan Balanced;
        public static readonly PowerPlan HighPerformance;

        #endregion

        #region Fields

        private static readonly Dictionary<Guid, PowerPlan> Cache;

        public readonly Guid Guid;
        public readonly string Name;
        public readonly string Description;

        #endregion

        static PowerPlan()
        {
            Cache = new Dictionary<Guid, PowerPlan>();
            PowerSaver = FromGuid("a1841308-3541-4fab-bc81-f71556f20b4a");
            Balanced = FromGuid("381b4222-f694-41f0-9685-ff5bb260df2e");
            HighPerformance = FromGuid("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
        }

        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        /// <param name="guid">The GUID of the power plan.</param>
        /// <param name="name">The name of the power plan.</param>
        /// <param name="desc">The description of the power plan.</param>
        private PowerPlan(Guid guid, string name, string desc)
        {
            Guid = guid;
            Name = name;
            Description = desc;
        }

        /// <summary>
        /// Sets this power plan as the active power plan.
        /// </summary>
        public void SetActive()
        {
            SetActivePowerPlan(this);
        }

        /// <summary>
        /// Gets the active power plan.
        /// </summary>
        public static PowerPlan GetActivePowerPlan()
        {
            return FromGuid(GetActivePowerPlanGuid());
        }

        /// <summary>
        /// Sets the active power plan.
        /// </summary>
        /// <param name="plan">The power plan to set.</param>
        private static void SetActivePowerPlan(PowerPlan plan)
        {
            Guid tempGuid = plan.Guid;
            if (PowerSetActiveScheme(IntPtr.Zero, ref tempGuid) != 0)
                throw new Win32Exception("Could not set the active power plan");
        }

        /// <summary>
        /// Gets a collection of all power plans on the computer.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<PowerPlan> GetPowerPlans()
        {
            return EnumeratePowerPlanGuids().Select(FromGuid);
        }

        /// <summary>
        /// Clears the cached power plans (use if a plan's name has changed).
        /// </summary>
        public static void ClearCache()
        {
            Cache.Clear();
        }

        /// <summary>
        /// Creates a PowerPlan structure using information from the registry.
        /// </summary>
        /// <param name="guid">The GUID of the power plan, in string form.</param>
        /// <exception cref="ArgumentException">Thrown if the power plan with the specified GUID was not found.</exception>
        public static PowerPlan FromGuid(string guid)
        {
            return FromGuid(new Guid(guid));
        }

        /// <summary>
        /// Creates a PowerPlan structure using information from the registry.
        /// </summary>
        /// <param name="guid">The GUID of the power plan.</param>
        /// <exception cref="ArgumentException">Thrown if the power plan with the specified GUID was not found.</exception>
        public static PowerPlan FromGuid(Guid guid)
        {
            PowerPlan plan;
            if (Cache.TryGetValue(guid, out plan)) return plan;
            string name = GetPowerPlanName(guid);
            string desc = GetPowerPlanDescription(guid);
            plan = new PowerPlan(guid, name, desc);
            Cache.Add(guid, plan);
            return plan;
        }

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <param name="other">Another object to compare to.</param>
        public bool Equals(PowerPlan other)
        {
            return Guid == other.Guid;
        }

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <param name="obj">Another object to compare to.</param>
        public override bool Equals(object obj)
        {
            if (!(obj is PowerPlan)) return false;
            return Equals((PowerPlan)obj);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode()
        {
            // ReSharper disable once ImpureMethodCallOnReadonlyValueField
            return Guid.GetHashCode();
        }

        public static bool operator ==(PowerPlan a, PowerPlan b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(PowerPlan a, PowerPlan b)
        {
            return !a.Equals(b);
        }

        private static Guid GetActivePowerPlanGuid()
        {
            IntPtr guidPtr;
            if (PowerGetActiveScheme(IntPtr.Zero, out guidPtr) != 0)
                throw new Win32Exception("Could not get the active power plan");
            try
            {
                return (Guid)Marshal.PtrToStructure(guidPtr, typeof(Guid));
            }
            finally
            {
                Marshal.FreeHGlobal(guidPtr);
            }
        }

        private static string GetPowerPlanDescription(Guid guid)
        {
            uint size = 0;
            if (PowerReadDescription(IntPtr.Zero, ref guid, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, ref size) != 0)
                throw new Win32Exception("Could not get description of power plan");
            IntPtr pDesc = Marshal.AllocHGlobal((int)size);
            try
            {
                if (PowerReadFriendlyName(IntPtr.Zero, ref guid, IntPtr.Zero, IntPtr.Zero, pDesc, ref size) != 0)
                    throw new Win32Exception("Could not get description of power plan");
                return Marshal.PtrToStringUni(pDesc);
            }
            finally
            {
                Marshal.FreeHGlobal(pDesc);
            }
        }

        private static string GetPowerPlanName(Guid guid)
        {
            uint size = 0;
            if (PowerReadFriendlyName(IntPtr.Zero, ref guid, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, ref size) != 0)
                throw new Win32Exception("Could not get name of power plan");
            IntPtr pName = Marshal.AllocHGlobal((int)size);
            try
            {
                if (PowerReadFriendlyName(IntPtr.Zero, ref guid, IntPtr.Zero, IntPtr.Zero, pName, ref size) != 0)
                    throw new Win32Exception("Could not get name of power plan");
                return Marshal.PtrToStringUni(pName);
            }
            finally
            {
                Marshal.FreeHGlobal(pName);
            }
        }

        private static IEnumerable<Guid> EnumeratePowerPlanGuids()
        {
            uint index = 0;
            Guid buffer = Guid.Empty;
            var bufferSize = (uint)Marshal.SizeOf(typeof(Guid));
            while (PowerEnumerate(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                AccessFlags.Scheme, index++, ref buffer, ref bufferSize) == 0)
            {
                yield return buffer;
            }
        }
    }
}