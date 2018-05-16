using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace IoTHubGateway.Server.Tests
{
    public class UtilsTest
    {
        [Fact]
        public void ResolveIoTHubHostName_UppercaseConnectionString_ReturnsLowerCaseHostName()
        {
            Assert.Equal("testaaa", Utils.ResolveIoTHubHostName("HOSTNAME=TESTAAA.AZURE-DEVICES.NET;DEVICEID=DEVICE0001;SHAREDACCESSKEY=ADASDSADASDAEXNWORJJ/85JY+6MB31H1Q4IS1TC1JKDASDKABWW="));

        }


        [Fact]
        public void ResolveIoTHubHostName_LowercaseConnectionString_ReturnsLowerCaseHostName()
        {
            Assert.Equal("testaaa", Utils.ResolveIoTHubHostName("hostname=testaaa.azure-devices.net;deviceid=device0001;sharedaccesskey=adasdsadasdaexnworjj/85jy+6mb31h1q4is1tc1jkdasdkabww="));
        }

        [Fact]
        public void ResolveIoTHubHostName_Different_Orders_Returns_Correct_Value()
        {
            Assert.Equal("testaaa", Utils.ResolveIoTHubHostName("Deviceid=device0001;sharedaccesskey=adasdsadasdaexnworjj/85jy+6mb31h1q4is1tc1jkdasdkabww=;HostName=testaaa.azure-devices.net"));
        }

        [Fact]
        public void ResolveIoTHubHostName_SomethingElse_As_Domain_Returns_Host_In_Name()
        {
            Assert.Equal("testaaa.somethingelse.net", Utils.ResolveIoTHubHostName("hostname=testaaa.somethingelse.net;deviceid=device0001;sharedaccesskey=adasdsadasdaexnworjj/85jy+6mb31h1q4is1tc1jkdasdkabww="));
        }

        [Fact]
        public void ResolveIoTHubHostName_Missing_Host_Name_Returns_Empty_String()
        {
            Assert.Equal("", Utils.ResolveIoTHubHostName("hostname31=testaaa.somethingelse.net;deviceid=device0001;sharedaccesskey=adasdsadasdaexnworjj/85jy+6mb31h1q4is1tc1jkdasdkabww="));
        }

        [Theory]
        [InlineData("", "device1", "device1")]
        [InlineData(null, "device1", "device1")]
        [InlineData("test", "device1", "test_device1")]
        public void UniqueDeviceID_ExpectedValues(string iotHubHostName, string deviceId, string expectedUniqueId)
        {
            Assert.Equal(expectedUniqueId, Utils.UniqueDeviceID(iotHubHostName, deviceId));
        }
    }
}
