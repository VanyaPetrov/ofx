using System;
using Mocoding.Ofx.Protocol;

namespace Mocoding.Ofx
{
    public static class OfxSerializer
    {
        public const string Default103Header =
       @"OFXHEADER:100
DATA:OFXSGML
VERSION:103
SECURITY:NONE
ENCODING:USASCII
CHARSET:1252
COMPRESSION:NONE
OLDFILEUID:NONE
NEWFILEUID:NONE

";

        private static readonly SgmlSerializer<OFX> SgmlSerializer = new SgmlSerializer<OFX>();

        public static string Serialize(OFX request)
        {
            var sgml = SgmlSerializer.Serialize(request);
            return Default103Header + sgml;
        }

        public static OFX Deserialize(string responseBody)
        {
            var ofxDataStartIndex = responseBody.IndexOf("<OFX>", StringComparison.OrdinalIgnoreCase);
            if (ofxDataStartIndex == -1)
                throw new FormatException("<OFX> element is not present in the response body");
            var sgml = responseBody.Substring(ofxDataStartIndex);

            var result = SgmlSerializer.Deserialize(sgml);

            return result;
        }
    }
}
