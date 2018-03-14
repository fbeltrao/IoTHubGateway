using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTHubGateway.Server
{
    public static class Constants
    {
        /// <summary>
        /// Name of request header containing the device sas token
        /// </summary>
        public const string SasTokenHeaderName = "sas_token";

        /// <summary>
        /// Name of request header containing the device sas token expiration date in unix time
        /// </summary>
        public const string SasTokenExpirationHeaderName = "sas_token_expiration";
        
    }
}
