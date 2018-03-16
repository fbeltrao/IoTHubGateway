using System;
using System.Runtime.Serialization;

namespace IoTHubGateway.Server.Services
{
    /// <summary>
    /// Exception raised when the device could not connected
    /// </summary>
    [Serializable]
    public class DeviceConnectionException : Exception
    {
        public DeviceConnectionException()
        {
        }

        public DeviceConnectionException(string message) : base(message)
        {
        }

        public DeviceConnectionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DeviceConnectionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}