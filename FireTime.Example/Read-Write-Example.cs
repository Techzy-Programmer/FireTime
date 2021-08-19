using System.Collections.Generic;
using System.Threading.Tasks;

namespace FireTime.Example
{
    public class Read_Write_Example
    {
        static readonly string Nl = System.Environment.NewLine;

        public static async Task Read(FireClient Client, string OPath)
        {
            Writer.Log("Trying to read.....");
            var FResp = await Client.ReadAsync(OPath);

            if (FResp.HasError)
            {
                Writer.Log($"{Nl}!!!! [Request Failed With Exception] !!!!{Nl}", LogType.Exception);
                Writer.Log($"[-] HTTP Status Code => {FResp.GetHttpStatusCode}{Nl}", LogType.Exception);
                Writer.Log($"[-] Error Message => {FResp.GetErrorMSG}{Nl}", LogType.Exception);
                Writer.Log($"~~~ [End Of Error Log] ~~~", LogType.Exception);
            }
            else
            {
                Writer.Log($"{Nl}>>>> [Request Was Successfull] <<<<{Nl}", LogType.Added);
                Writer.Log($"[I] HTTP Status Code => {FResp.GetHttpStatusCode}{Nl}", LogType.Added);
                Writer.Log($"[I] Data Read From The Server Are ::{Nl}", LogType.Added);

                if (FResp.IsConvertible)
                {
                    // Here you can safely access the GetAs<T>(), GetAsJObject property
                    foreach (var JsonKV in FResp.GetAsJObject.Properties())
                        Writer.Log($"[-] {JsonKV.Name} = {JsonKV.Value}{Nl}", LogType.Added);
                }
                else Writer.Log(FResp.RawJSON + Nl, LogType.Added);
                Writer.Log($"~~~ [End Of Request Response] ~~~{Nl}", LogType.Added);
            }
        }

        public static async Task Write(FireClient Client, string WPath)
        {
            object DataToWrite;
            Writer.Log("Trying To Write Data.....");
            #region More Ways To Write Data To The Firebase Realtime Database
            /* DataToWrite = new // You can also use Anonymous object to write to the server
            {
                LoopObject = new
                {
                    Loop1 = "Value 1",
                    Loop2 = "Other Data",
                    Loop3 = 90649086635435689
                },

                StringKey = "Value",
                BoolKey = true,
                IntKey = 9068
            };

            DataToWrite = new CustomClassForWrite // You can also use a Custom typed object to write to the server
            {
                StringToWrite = "Value To Add",
                BoolToWrite = false,
                IntToWrite = 86490
            };
            var CustomWritable = (CustomClassForWrite)DataToWrite;
            CustomWritable.OBJToWrite.Dynamic_Dictionary = new Dictionary<string, object> { { "Add data", "Here" } }; // Add more data here
            CustomWritable.OBJToWrite.Dynamic_AnonymousKey = new { NestedKey1 = "Data", NestedKey2 = 90.888F }; // etc....
            CustomWritable.OBJToWrite.Dynamic_StringKey = "Value of dyn-1"; // etc......

            DataToWrite = "{\"Direct Data\":\"Raw Value\", \"Other Data\":[\"String in array\", true, 0.9808, 90]}"; // You can even directly write data as a valid JSON string also */
            #endregion

            DataToWrite = new Dictionary<string, object> // A Dictionary object to write to the server [Always use string as Key type]
            {
                { "String Key", "Value AS String" },
                { "Bool Key", true },
                { "Int Key", 6008 },

                { "Object Key",
                    new Dictionary<string, object>
                    {
                        { "Data-1", "Sample Value" },
                        { "Data-2", false }
                    }
                },

                { "Array Key",
                    new Dictionary<string, object> // Using Integers as key will create a JSON array on the Firebase Server with Int as index
                    { // If you add string type as key along with integers then firebase will treat the data as JSON object
                        { "0", "I am string" },
                        { "1", 0.0964408 },
                        { "2", 907500 },
                        { "5", true }, // Array index with 5 having a valid value will be added after two null values as we have skipped 3 and 4 here, but this will not be visible in the console view of Firebase panel
                        { "6", null } // This data will not be written on server as it's value is null
                    }
                }
            };
            var FResp = await Client.WriteAsync(WPath, DataToWrite);

            if (FResp.HasError)
            {
                Writer.Log($"{Nl}!!!! [Request Failed With Exception] !!!!{Nl}", LogType.Exception);
                Writer.Log($"[-] HTTP Status Code => {FResp.GetHttpStatusCode}{Nl}", LogType.Exception);
                Writer.Log($"[-] Error Message => {FResp.GetErrorMSG}{Nl}", LogType.Exception);
                Writer.Log($"~~~ [End Of Error Log] ~~~", LogType.Exception);
            }
            else
            {
                Writer.Log($"{Nl}>>>> [Request Was Successfull] <<<<{Nl}", LogType.Added);
                Writer.Log($"[I] HTTP Status Code => {FResp.GetHttpStatusCode}{Nl}", LogType.Added);
                Writer.Log($"Here Is The Data Written To The Server:{Nl}", LogType.Added);
                Writer.Log(FResp.RawJSON + Nl, LogType.Added);
                Writer.Log($"~~~ [End Of Request Response] ~~~{Nl}", LogType.Added);
            }
        }
    }

    public class CustomClassForWrite
    {
        public string StringToWrite { get; set; }
        public dynamic OBJToWrite { get; set; }
        public bool BoolToWrite { get; set; }
        public int IntToWrite { get; set; }
    }
}