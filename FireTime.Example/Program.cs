using System;

namespace FireTime.Example
{
    public static class Program
    {
        public static void Main(string[] _)
        {
            AppDomain.CurrentDomain.UnhandledException += HandleGER;

            /* Managed and created by Rishabh Kumar
             * Highly structured and well formatted samples are available in this project
             * This program is created only to display the working mechanism of FireTime library
             * [Don't forget to configure FireClient before running samples]
             */

            /* Please configure the following settings correctly or else you may get error on the next step */
            var FConfigs = new FireConfig // Initialize a new class for Firebase configuarations
            {
                FirebaseURL = "https://{your-databse-id}.{firebase-domain}.{extension}", // [Required]
                AuthToken = "Your unique firebase auth or access_token", // [Optional] Remove this field if not required
                BearerToken = "Bearer token from custom authorizations" // [Optional] Remove this field if not required
            };

            var FClient = new FireClient(FConfigs); // You may get an error if the provided Firebase URL is not in a valid format

            /* Uncomment any of the following line to run the samples accordingly */
            /* ------------------------------------------------------------------ */
            // Stream_API_Example.Run(FClient, "Path/To/Monitor"); // Monitor for any changes to the data at specified path upto One Million nested nodes!
            // Update_Remove_Example.Update(FClient, "Path/To/Update").Wait(); // Replace values corresponding to the sent key value paired request
            // Update_Remove_Example.Remove(FClient, "Path/To/Delete").Wait(); // Erase data on the specified path with all child and nested nodes
            // Read_Write_Example.Read(FClient, "Path/To/Read").Wait(); // Read all data from the specified path as a json string from the server
            // Read_Write_Example.Write(FClient, "Path/To/Write").Wait(); // [Warning] Writing data will overwrite existing values stored on the server

            Writer.Log(Environment.NewLine + "Program Terminated. [Hit Enter To Exit]");
            Console.ReadLine();
        }

        private static void HandleGER(object _, UnhandledExceptionEventArgs EArgs) // Just a simple but universal exception catcher
        {
            var Ep = (Exception)EArgs.ExceptionObject;
            System.IO.File.AppendAllLines("Error-Logs.txt", new string[]
            { string.Empty, Ep.GetType().FullName, Ep.Message, Ep.StackTrace });
        }
    }

    public static class Writer
    {
        public static void Log(string WText, LogType LType = LogType.None)
        {
            ConsoleColor Clr = ConsoleColor.Cyan;

            if (LType == LogType.Added) Clr = ConsoleColor.Green;
            else if (LType == LogType.Removed) Clr = ConsoleColor.Red;
            else if (LType == LogType.Exception) Clr = ConsoleColor.Magenta;
            else if (LType == LogType.Updated) Clr = ConsoleColor.DarkYellow;

            Console.ForegroundColor = Clr;
            Console.WriteLine(WText);
            Console.ResetColor();
        }
    }

    public enum LogType
    {
        None,
        Added,
        Updated,
        Removed,
        Exception
    }
}
