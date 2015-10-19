//TODO: Add more properties (see site below)
//http://msdn.microsoft.com/en-us/library/windows/desktop/aa394418%28v=vs.85%29.aspx

using System;
using System.Collections.Generic;
using System.Management;
using System.ServiceProcess;

namespace PowerPlanChanger
{
    /// <summary>
    /// Wraps a ServiceController instance and exposes 
    /// additional properties from the Win32_Service WMI class.
    /// </summary>
    public class Service : IDisposable
    {
        private static readonly Dictionary<string, Service> ServiceCache = new Dictionary<string, Service>();

        private readonly string _serviceName;
        private ServiceController _serviceController;
        private ManagementBaseObject _queryObject;
        private bool _disposed;
        private string _displayName;
        private readonly Dictionary<string, object> _propertyCache = new Dictionary<string, object>();

        /// <summary>
        /// Gets the unique service name (e.g. wuauserv).
        /// </summary>
        public string ServiceName
        {
            get { return _serviceName; }
        }

        /// <summary>
        /// Gets the user-friendly service name (e.g. Windows Update).
        /// </summary>
        public string DisplayName
        {
            get { return _displayName ?? (_displayName = ServiceController.DisplayName); }
        }

        /// <summary>
        /// Gets the status of the service.
        /// </summary>
        public ServiceControllerStatus Status
        {
            get
            {
                ServiceController.Refresh();
                return ServiceController.Status;
            }
        }

        /// <summary>
        /// Gets whether the service is running.
        /// </summary>
        public bool IsRunning
        {
            get
            {
                ServiceControllerStatus status = Status;
                return status == ServiceControllerStatus.Running ||
                       status == ServiceControllerStatus.StartPending;
            }
        }

        /// <summary>
        /// Gets the description of the service.
        /// </summary>
        public string Description
        {
            get
            {
                object desc = GetProperty("Description");
                return desc != null ? string.Intern(desc.ToString()) : string.Empty;
            }
        }

        /// <summary>
        /// Gets a property of the service.
        /// </summary>
        /// 
        /// <param name="propertyName">The name of the property.</param>
        public object this[string propertyName]
        {
            get { return GetProperty(propertyName); }
        }

        /// <summary>
        /// Gets the ServiceController instance that this class wraps.
        /// </summary>
        private ServiceController ServiceController
        {
            get
            {
                AssertNotDisposed();
                return _serviceController ?? (_serviceController = new ServiceController(_serviceName));
            }
        }

        /// <summary>
        /// Gets the object that lets us query the service's properties.
        /// </summary>
        private ManagementBaseObject QueryObject
        {
            get
            {
                if (_queryObject != null) return _queryObject;
                string filter = "SELECT * FROM Win32_Service WHERE Name = \"" + _serviceName + "\"";
                using (var query = new ManagementObjectSearcher(filter))
                using (ManagementObjectCollection services = query.Get())
                using (var e = services.GetEnumerator())
                {
                    e.MoveNext();
                    return _queryObject = e.Current;
                }
            }
        }

        /// <summary>
        /// Creates a new instance of this class from the name of the service to wrap.
        /// </summary>
        /// <param name="serviceName">The unique name of the service.</param>
        private Service(string serviceName)
        {
            _serviceName = serviceName;
        }

        /// <summary>
        /// Creates a new instance of this class using the ServiceController class. 
        /// This must only be used if the ServiceController will not be modified after this point.
        /// </summary>
        /// <param name="svc">An instance of the ServiceController class.</param>
        private Service(ServiceController svc)
        {
            _serviceName = svc.ServiceName;
            _serviceController = svc;
        }

        /// <summary>
        /// Gets a service with the specified name.
        /// </summary>
        /// <param name="name">The unique name of the service.</param>
        public static Service FromName(string name)
        {
            Service svc;
            if (!ServiceCache.TryGetValue(name, out svc))
            {
                svc = new Service(name);
                ServiceCache.Add(name, svc);
            }
            return svc;
        }

        /// <summary>
        /// Gets a service from a ServiceController instance.
        /// </summary>
        /// <param name="service">The service.</param>
        public static Service FromServiceController(ServiceController service)
        {
            bool newInstance;
            return FromServiceController(service, true, out newInstance);
        }

        /// <summary>
        /// Gets a service from a ServiceController instance.
        /// </summary>
        /// <param name="service">The service.</param>
        /// <param name="copy">Whether to create a copy of the ServiceController.</param>
        /// <param name="newInstance">Whether a new instance was created or a cached one was used.</param>
        private static Service FromServiceController(ServiceController service, bool copy, out bool newInstance)
        {
            string name = service.ServiceName;
            Service newService;
            if (!ServiceCache.TryGetValue(name, out newService))
            {
                newService = copy ? new Service(name) : new Service(service);
                ServiceCache.Add(name, newService);
                newInstance = true;
            }
            else
            {
                newInstance = false;
            }
            return newService;
        }

        /// <summary>
        /// Gets a collection of all services on the local computer.
        /// </summary>
        public static Service[] GetServices()
        {
            ServiceController[] origSvcs = ServiceController.GetServices();
            var services = new Service[origSvcs.Length];
            for (int i = 0; i < services.Length; ++i)
            {
                ServiceController svc = origSvcs[i];
                bool newInstance;
                services[i] = FromServiceController(svc, false, out newInstance);
                if (!newInstance) svc.Dispose();
            }
            return services;
        }

        /// <summary>
        /// Refreshes all services' property caches.
        /// </summary>
        public static void RefreshAllProperties()
        {
            foreach (Service cachedService in ServiceCache.Values)
                cachedService.RefreshProperties();
        }

        /// <summary>
        /// Ensures that the object has not been disposed.
        /// </summary>
        private void AssertNotDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(GetType().Name);
        }

        /// <summary>
        /// Starts the service.
        /// </summary>
        public void Start()
        {
            if (!IsRunning) ServiceController.Start();
        }

        /// <summary>
        /// Stops the service.
        /// </summary>
        public void Stop()
        {
            if (IsRunning) ServiceController.Stop();
        }

        /// <summary>
        /// Refreshes the service's property cache.
        /// </summary>
        public void RefreshProperties()
        {
            AssertNotDisposed();
            _propertyCache.Clear();
        }

        /// <summary>
        /// Gets a property of the service.
        /// </summary>
        /// 
        /// <param name="propertyName">The name of the property.</param>
        /// 
        /// <exception cref="ArgumentException">Thrown when an invalid property name is provided.</exception>
        /// <exception cref="ArgumentNullException">Thrown if the property name is null.</exception>
        public object GetProperty(string propertyName)
        {
            AssertNotDisposed();
            object value;
            if (!_propertyCache.TryGetValue(propertyName, out value))
            {
                try
                {
                    value = QueryObject[propertyName];
                    _propertyCache.Add(propertyName, value);
                }
                catch (ManagementException ex)
                {
                    throw new ArgumentException("Invalid property name provided", ex);
                }
            }

            return value;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                try
                {
                    ServiceCache.Remove(ServiceName);
                }
                catch (InvalidOperationException)
                {
                    //Service doesn't exist anymore, but it doesn't matter
                }
                if (_serviceController != null)
                    _serviceController.Dispose();
            }
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Service()
        {
            Dispose(false);
        }
    }
}