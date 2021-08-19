using System;
using FireTime.Response;
using Newtonsoft.Json.Linq;

namespace FireTime.Example
{
    public class Stream_API_Example
    {
        static string LPath = string.Empty;
        static StreamResponse Listner = null;
        static readonly string Nl = Environment.NewLine;

        public static void Run(FireClient Client, string ListenPath)
        {
            LPath = ListenPath;
            Listner = Client.AttachListner(ListenPath); // Create a new listner for the specified path in your database
            Listner.Changes.OnMonitoringStarted += OnMonitoringStarted; // Fires whenever monitoring starts or restarts
            Listner.Changes.OnError += OnError; // [Subscribe for better tracking] Listen for any errors that might occur during streaming
            Listner.Changes.OnAdded += OnAdded; // Listen to all Addition event that take place in your database at any sub-level
            Listner.Changes.OnUpdated += OnUpdated; // Listen to all Updation event that take place in your database at any sub-level
            Listner.Changes.OnRemoved += OnRemoved; // Listen to all Deletion event that take place in your database at any sub-level

            Console.ReadLine(); Listner.Detach(); // Dispose and close listening when user hits enter on console window
            Console.WriteLine("Listner Stopped!");
        }

        private static void OnError(Exception FailExep)
        {
            Writer.Log($"{Nl}!!!!! (Exception Occured) !!!!!{Nl}" +
                $"Type => {FailExep.GetType().FullName}{Nl}" +
                $"Message => {FailExep.Message}{Nl}" +
                $"Stacktrace => {FailExep.StackTrace}{Nl}" +
                $"~~~ [End Of Exception] ~~~", LogType.Exception);
        }

        private static void OnAdded(AddedEventArgs AEArgs)
        {
        //    var Data = Listner.RootDataBranch;
        //    if (Data != null) Writer.Log(Data.ToString(), LogType.Updated);
        //    return;
            Writer.Log($"{Nl}++++++ Addition Event ++++++{Nl}" +
                $"At Path => {AEArgs.Path}{Nl}", LogType.Added);

            if (AEArgs.AddedData is JObject NewOBJ)
            {
                // You can also use indexer to access the properties of new data
                // Like NewData["Your-Property Key of json"];
                // Use this method only if NewData is JObject

                foreach (var NProp in NewOBJ.Properties())
                    Writer.Log($"[New-Data] => {NProp.Name} = {NProp.Value}", LogType.Added);
            }
            else Writer.Log($"[New-Data] => {AEArgs.AddedData}", LogType.Added);

            Writer.Log($"{Nl}~~~ [End Of Event] ~~~", LogType.Added);
        }

        private static void OnUpdated(UpdatedEventArgs UEArgs)
        {
            //var Data = Listner.RootDataBranch;
            //if (Data != null) Writer.Log(Data.ToString(), LogType.Updated);
            //return;

            Writer.Log($"{Nl}-+-+-+ Updates Event +-+-+-{Nl}" +
                $"At Path => {UEArgs.Path}{Nl}", LogType.Updated);

            if (UEArgs.OldData is JObject OldOBJ)
            {
                // You can also use indexer to access the properties of new data
                // Like OldData["Your-Property Key of json"];
                // Use this method only if OldData is JObject

                foreach (var OProp in OldOBJ.Properties())
                    Writer.Log($"[Old-Data] => {OProp.Name} = {OProp.Value}", LogType.Updated);
            }
            else Writer.Log($"[Old-Data] => {UEArgs.OldData}", LogType.Updated);

            if (UEArgs.UpdatedData is JObject UpOBJ)
            {
                // You can also use indexer to access the properties of new data
                // Like UpdatedData["Your-Property Key of json"];
                // Use this method only if UpdatedData is JObject

                foreach (var UProp in UpOBJ.Properties())
                    Writer.Log($"[Updated-Data] => {UProp.Name} = {UProp.Value}", LogType.Updated);
            }
            else Writer.Log($"[Updated-Data] => {UEArgs.UpdatedData}", LogType.Updated);

            Writer.Log($"{Nl}~~~ [End Of Event] ~~~", LogType.Updated);
        }

        private static void OnRemoved(RemovedEventArgs REArgs)
        {
            //var Data = Listner.RootDataBranch;
            //if (Data != null) Writer.Log(Data.ToString(), LogType.Updated);
            //return;

            Writer.Log($"{Nl}------ Removed Event ------{Nl}" +
                $"At Path => {REArgs.Path}{Nl}", LogType.Removed);

            if (REArgs.PreviousData is JObject PrevOBJ)
            {
                // You can also use indexer to access the properties of new data
                // Like PreviousData["Your-Property Key of json"];
                // Use this method only if PreviousData is JObject

                foreach (var PProp in PrevOBJ.Properties())
                    Writer.Log($"[Previous-Data] => {PProp.Name} = {PProp.Value}", LogType.Removed);
            }
            else Writer.Log($"[Previous-Data] => {REArgs.PreviousData}", LogType.Removed);

            Writer.Log($"{Nl}~~~ [End Of Event] ~~~", LogType.Removed);
        }

        private static void OnMonitoringStarted(bool HasRestarted)
            => Console.WriteLine($"Monitoring Path : {LPath}");
    }
}