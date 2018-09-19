using System.Threading.Tasks;
using Mocoding.Ofx.Client.Helpers;
using Mocoding.Ofx.Client.Interfaces;
using Mocoding.Ofx.Client.Models;

namespace Mocoding.Ofx.Client
{
    public static class OfxClientExtensions
    {
        public static async Task<AccountTransactions> GetTransactions(this IOfxClient client, Account account, string customResponseBody)
        {
            var options = client.Options;
            var clientRequestProxy = new OfxClient(new OfxClientHelperRequestProxy(options, customResponseBody));
            return await clientRequestProxy.GetTransactions(account);
        }
    }
}