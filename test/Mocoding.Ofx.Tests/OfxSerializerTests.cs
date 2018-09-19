using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using Xunit;

namespace Mocoding.Ofx.Tests
{
    public class OfxSerializerTests
    {
        //private OfxSerializer _serializer;
        //public OfxSerializerTests()
        //{
          //  _serializer = new OfxSerializer();
        //}

        [Fact]
        public void AccountListRequestTest()
        {
            var response = EmbeddedResourceReader.ReadRequestAsString("accountList.sgml");
            var ofx = OfxSerializer.Deserialize(response);
            var serialized = OfxSerializer.Serialize(ofx);

            Assert.Equal(response, serialized);
        }

        [Fact]
        public void AccountListResponseTest()
        {
            var response = EmbeddedResourceReader.ReadResponseAsString("accountList.sgml");
            var ofx = OfxSerializer.Deserialize(response);
            var serialized = OfxSerializer.Serialize(ofx);

            Assert.Equal(response, serialized);
        }
    }
}
