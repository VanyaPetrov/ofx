using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Mocoding.Ofx.Client.Components;
using Mocoding.Ofx.Client.Exceptions;
using Mocoding.Ofx.Client.Interfaces;
using Mocoding.Ofx.Protocol;

[assembly: InternalsVisibleTo("Mocoding.Ofx.Client.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
namespace Mocoding.Ofx.Client.Helpers
{
    public class OfxClientHelper : IOfxClientHelper
    {
        readonly IOfxClientTransport _transport;
        readonly IUtils _utils;
        private OfxClientOptions Options { get; }

        public OfxClientHelper(OfxClientOptions options)
        {
            Options = options;
            _transport = new TcpClientTransport();
            _utils = new Utils();
        }

        internal OfxClientHelper(OfxClientOptions options, IOfxClientTransport transport, IUtils utils)
        {
            Options = options;
            _transport = transport;
            _utils = utils;
        }


        public virtual async Task<TResponseMessage> ExecuteRequest<TRequestMessage, TResponseMessage>(TRequestMessage accountListRequest)
            where TRequestMessage : AbstractRequestMessageSet
            where TResponseMessage : AbstractResponseMessageSet
        {
            var request = CreatedRequest();
            request.Add(accountListRequest);

            var ofxRequest = new OFX() { Items = request.ToArray() };
            var requestBody = OfxSerializer.Serialize(ofxRequest);
            if (requestBody == null)
                throw new OfxSerializationException("Request serialization error.");

            var responseBody = await _transport.PostRequest(Options.ApiUrl, requestBody);
            var messageSet = ParseResponseMessage<TResponseMessage>(responseBody);
            return messageSet;
        }

        internal TResponseMessage ParseResponseMessage<TResponseMessage>(string message)
            where TResponseMessage : AbstractResponseMessageSet
        {
            var ofxResponse = OfxSerializer.Deserialize(message);
            if (ofxResponse == null)
                throw new OfxSerializationException("Deserialization error.");

            var signInResponse = ofxResponse.Items.FirstOrDefault(_ => _ is SignonResponseMessageSetV1) as SignonResponseMessageSetV1;
            if (signInResponse == null)
                throw new OfxResponseException("SIGNONRESPONSEMESSAGESETV1 is not present in response.");

            if (signInResponse.SONRS.STATUS.CODE != "0")
                throw new OfxResponseException(signInResponse.SONRS.STATUS.MESSAGE);

            var messageSet = ofxResponse.Items.FirstOrDefault(_ => _ is TResponseMessage) as TResponseMessage;

            if (messageSet == null)
                throw new OfxResponseException("Requested message set " + typeof(TResponseMessage).Name.ToUpper() + " is not present in response.");

            return messageSet;
        }

        private List<AbstractTopLevelMessageSet> CreatedRequest()
        {
            return new List<AbstractTopLevelMessageSet>()
            {
                new SignonRequestMessageSetV1()
                {
                    SONRQ = new SignonRequest()
                    {
                        CLIENTUID = _utils.GetClientUid(Options.UserId),
                        DTCLIENT = _utils.GetCurrentDateTime(),
                        LANGUAGE = LanguageEnum.ENG,
                        FI =
                            new FinancialInstitution()
                            {
                                FID = Options.BankFid,
                                ORG = Options.BankOrg
                            },
                        APPID = "QWIN",
                        APPVER = "2500",
                        Items = new[] {Options.UserId, Options.Password},
                        ItemsElementName = new[] {ItemsChoiceType.USERID, ItemsChoiceType.USERPASS}
                    }
                }
            };

        }
    }
}
