using System.Threading.Tasks;

namespace Mocoding.Ofx.Client.Helpers
{
    public class OfxClientHelperRequestProxy : OfxClientHelper
    {
        private string CustomResponseBody { get; }

        public OfxClientHelperRequestProxy(OfxClientOptions options, string customResponseBody) : base(options)
        {
            CustomResponseBody = customResponseBody;
        }

        public override async Task<TResponseMessage> ExecuteRequest<TRequestMessage, TResponseMessage>(TRequestMessage accountListRequest)
        {
            var messageSet = ParseResponseMessage<TResponseMessage>(CustomResponseBody);
            return await Task.FromResult(messageSet);
        }
    }
}