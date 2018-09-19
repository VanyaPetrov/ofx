using System.Threading.Tasks;
using Mocoding.Ofx.Protocol;

namespace Mocoding.Ofx.Client.Helpers
{
    public interface IOfxClientHelper
    {
        Task<TResponseMessage> ExecuteRequest<TRequestMessage, TResponseMessage>(TRequestMessage accountListRequest)
            where TRequestMessage : AbstractRequestMessageSet
            where TResponseMessage : AbstractResponseMessageSet;
    }
}