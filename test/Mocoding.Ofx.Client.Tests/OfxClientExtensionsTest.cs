using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Mocoding.Ofx.Client.Helpers;
using Mocoding.Ofx.Client.Interfaces;
using Mocoding.Ofx.Client.Models;
using Mocoding.Ofx.Tests;
using NSubstitute;
using Xunit;

namespace Mocoding.Ofx.Client.Tests
{
    public class OfxClientExtensionsTest
    {

        readonly Uri ApiUrl = new Uri("http://localhost:5000/api/ofx");

        [Fact]
        public async Task BankTransactionsListTest()
        {
            var expectedResponse =
                EmbeddedResourceReader.ReadResponseAsString("bankTransactions.sgml");

            var options = new OfxClientOptions(ApiUrl, "HAN", "5959", "testUserAccount", "testUserPassword");

            var client = new OfxClient(new OfxClientHelper(options));
            var account = new Account(AccountTypeEnum.Checking, "YYYYYYYY1924", "XXXXXXXXX");

            var transactions = await client.GetTransactions(account, expectedResponse);

            Assert.NotNull(transactions);
            Assert.Equal(2, transactions.Items.Length);
        }
    }
}
