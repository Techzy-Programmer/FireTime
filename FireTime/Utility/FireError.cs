using System;

namespace FireTime
{
    /// <summary>
    /// FireTime custom error class derived from base System.Exception class
    /// </summary>
    public class FireError : Exception
    {
        private readonly string EMsg;

        /// <summary>
        /// Get the error message specific to the current error
        /// </summary>
        public override string Message => EMsg;

        internal FireError() { }
        internal FireError(string _Msg) => EMsg = _Msg;
    }
}
