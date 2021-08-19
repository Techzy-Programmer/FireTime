using System;
using System.Threading.Tasks;

namespace FireTime.Example
{
    public class Update_Remove_Example
    {
        static readonly string Nl = Environment.NewLine;

        public static async Task Update(FireClient Client, string UPath)
        {
            object DataToUpdate;
            Writer.Log("Trying To Update Data.....");
            #region More Ways To Write Data To The Firebase Realtime Database
            /* DataToUpdate = new Dictionary<string, object> // You can use a Dictionary object to update values to the server [Always use string as Key type]
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

            DataToUpdate = new CustomClassForWrite // You can also use a Custom typed object to update data to the server
            {
                StringToWrite = "Value To Add",
                BoolToWrite = false,
                IntToWrite = 86490
            };
            var CustomWritable = (CustomClassForWrite)DataToUpdate;
            CustomWritable.OBJToWrite.Dynamic_Dictionary = new Dictionary<string, object> { { "Add data", "Here" } }; // Add more data here
            CustomWritable.OBJToWrite.Dynamic_AnonymousKey = new { NestedKey1 = "Data", NestedKey2 = 90.888F }; // etc....
            CustomWritable.OBJToWrite.Dynamic_StringKey = "Value of dyn-1"; // etc......

            DataToUpdate = "{\"Direct Data\":\"Raw Value\", \"Other Data\":[\"String in array\", true, 0.9808, 90]}"; // You can even directly update data using a valid JSON string also */
            #endregion

            DataToUpdate = new // An Anonymous object to update data on the server
            { // If data with key specified in this request doesn't already exists on the Firebase server then a new record would be created
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

            var FResp = await Client.UpdateAsync(UPath, DataToUpdate);

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
                Writer.Log($"Here Is The Data Updated On The Server:{Nl}", LogType.Added);
                Writer.Log(FResp.RawJSON + Nl, LogType.Added);
                Writer.Log($"~~~ [End Of Request Response] ~~~{Nl}", LogType.Added);
            }
        }

        public static async Task Remove(FireClient Client, string DPath)
        {
            Writer.Log("Trying to delete.....");
            var FResp = await Client.RemoveAsync(DPath);

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
                Writer.Log($"Data From The Server Has Been Removed{Nl}", LogType.Added);
                Writer.Log($"~~~ [End Of Request Response] ~~~{Nl}", LogType.Added);
            }
        }
    }
}
