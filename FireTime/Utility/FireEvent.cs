using System;
using Newtonsoft.Json.Linq;

namespace FireTime
{
    /// <summary>
    /// Listen for the events from the live streaming API of Firebase
    /// </summary>
    public class FireStreamEvent
    {
        internal bool HasStopped = false;
        internal bool HasPrevented = true;

        /// <summary>
        /// <para>Throws when unhandable exception occurs in the application</para>
        /// <para>You will get an exception object for further debugging</para>
        /// </summary>
        public event Error OnError;

        /// <summary>
        /// <para>Fires when a new data is added to the monitoring path provided by you.</para>
        /// <para>A (string) containing Path and a (JToken) containing new data will be dispatched with this event</para>
        /// </summary>
        public event Added OnAdded;

        /// <summary>
        /// <para>Fires when any existing path inside the monitoring path is replaced by new data or when updated.</para>
        /// <para>A (string) containing Path a (JToken) containing old data and a (JToken) containing updated data will be dispatched with this event</para>
        /// </summary>
        public event Updated OnUpdated;

        /// <summary>
        /// <para>Fires whenever any existing data is removed from the server</para>
        /// <para>A (string) containing Path and a (JToken) containing previous data will be dispatched with this event</para>
        /// </summary>
        public event Removed OnRemoved;

        /// <summary>
        /// Fires whenever the listner starts to monitor for the db changes or whenever the listner restarts the monitoring
        /// </summary>
        public event MonitoringStarted OnMonitoringStarted;

#pragma warning disable CS1591
        public delegate void Error(Exception FailExep);
        public delegate void Added(AddedEventArgs AEArgs);
        public delegate void Updated(UpdatedEventArgs UEArgs);
        public delegate void Removed(RemovedEventArgs REArgs);
        public delegate void MonitoringStarted(bool HasRestarted);
#pragma warning restore CS1591

        internal FireStreamEvent() { }

        internal void WarnError(Exception Exep)
        {
            if (HasStopped) return;
            OnError?.Invoke(Exep);
        }

        internal void NotifyMonitoring(bool IsRestarted)
        {
            if (HasStopped) return;
            OnMonitoringStarted?.Invoke(IsRestarted);
        }

        internal void NotifyAdded(string IPath, JToken IAddToken)
        {
            if (HasPrevented || HasStopped) return;
            OnAdded?.Invoke(new AddedEventArgs(IPath, IAddToken));
        }

        internal void NotifyUpdated(string IPath, JToken IOld, JToken IUpdated)
        {
            if (HasPrevented || HasStopped) return;
            OnUpdated?.Invoke(new UpdatedEventArgs(IPath, IOld, IUpdated));
        }

        internal void NotifyRemoved(string IPath, JToken IPrevious)
        {
            if (HasPrevented || HasStopped) return;
            OnRemoved?.Invoke(new RemovedEventArgs(IPath, IPrevious));
        }
    }

    /// <summary>
    /// Holds the data and information for the Added event
    /// </summary>
    public class AddedEventArgs
    {
        internal AddedEventArgs() { }

        internal AddedEventArgs(string _Path, JToken _AddedData)
        {
            Path = _Path;
            AddedData = _AddedData;
        }

        /// <summary>
        /// Get the relative Path to the resource which has been altered
        /// Path strings are seperated by the Firebase specific delimiter character '/'
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// Get the newly added data for the specified path
        /// </summary>
        public JToken AddedData { get; private set; }
    }

    /// <summary>
    /// Holds the data and information for the Updated event
    /// </summary>
    public class UpdatedEventArgs
    {
        internal UpdatedEventArgs() { }

        internal UpdatedEventArgs(string _Path, JToken _OldData, JToken _UpdatedData)
        {
            Path = _Path;
            OldData = _OldData;
            UpdatedData = _UpdatedData;
        }

        /// <summary>
        /// Get the relative Path to the resource which has been altered
        /// Path strings are seperated by the Firebase specific delimiter character '/'
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// Get the old/previous data associated with the resource at the provided path
        /// </summary>
        public JToken OldData { get; private set; }

        /// <summary>
        /// Get the newly updated data for the specified path
        /// </summary>
        public JToken UpdatedData { get; private set; }
    }

    /// <summary>
    /// Holds the data and information for the Removed event
    /// </summary>
    public class RemovedEventArgs
    {
        internal RemovedEventArgs() { }

        internal RemovedEventArgs(string _Path, JToken _PreviousData)
        {
            Path = _Path;
            PreviousData = _PreviousData;
        }

        /// <summary>
        /// Get the relative Path to the resource which has been altered
        /// Path strings are seperated by the Firebase specific delimiter character '/'
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// Get the retrieved data for the specified path before being removed
        /// </summary>
        public JToken PreviousData { get; private set; }
    }
}