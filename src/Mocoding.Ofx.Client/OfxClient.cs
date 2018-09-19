using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Mocoding.Ofx.Client.Components;
using Mocoding.Ofx.Client.Exceptions;
using Mocoding.Ofx.Client.Helpers;
using Mocoding.Ofx.Client.Interfaces;
using Mocoding.Ofx.Client.Models;
using Mocoding.Ofx.Protocol;

[assembly: InternalsVisibleTo("Mocoding.Ofx.Client.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace Mocoding.Ofx.Client
{
    public class OfxClient : IOfxClient
    {
        readonly IUtils _utils;
        private readonly IOfxClientHelper _clientHelper;

        public OfxClientOptions Options { get; }

        public OfxClient(OfxClientOptions options)
            : this(options, new Utils())
        {

        }

        public OfxClient(IOfxClientHelper clientHelper)
            : this(clientHelper, new Utils())
        {

        }

        internal OfxClient(OfxClientOptions options, IUtils utils)
            : this(new OfxClientHelper(options), utils)
        {
            Options = options;
        }

        private OfxClient(IOfxClientHelper clientHelper, IUtils utils)
        {
            _utils = utils;
            _clientHelper = clientHelper;
        }

        public async Task<Account[]> GetAccounts()
        {
            var accountsRequest = new SignupRequestMessageSetV1()
            {
                Items = new AbstractRequest[]
                {
                    new AccountInfoTransactionRequest()
                    {
                       TRNUID = _utils.GenerateTransactionId(),
                       CLTCOOKIE = "4",
                       ACCTINFORQ = new AccountInfoRequest(){DTACCTUP = "19900101000000"}
                    }
                }
            };

            var messageSet = await _clientHelper.ExecuteRequest<SignupRequestMessageSetV1, SignupResponseMessageSetV1>(accountsRequest);
            var accountsResponse =
                messageSet.Items.FirstOrDefault(_ => _ is AccountInfoTransactionResponse) as AccountInfoTransactionResponse;

            if (accountsResponse == null)
                throw new OfxResponseException("Required response is not present in message set.");

            var result = new List<Account>();
            foreach (var accountInfo in accountsResponse.ACCTINFORS.ACCTINFO)
            {
                AccountTypeEnum type;
                string subtype = null;
                string status = null;
                string accountId = null;
                string bankId = null;

                var bankAccount = accountInfo.Items.FirstOrDefault(_ => _ is BankAccountInfo) as BankAccountInfo;
                var ccAccount =
                    accountInfo.Items.FirstOrDefault(_ => _ is CreditCardAccountInfo) as CreditCardAccountInfo;

                if (bankAccount != null)
                {
                    type = AccountTypeEnum.Checking;
                    accountId = bankAccount.BANKACCTFROM.ACCTID;
                    bankId = bankAccount.BANKACCTFROM.BANKID;
                    subtype = bankAccount.BANKACCTFROM.ACCTTYPE.ToString();
                    status = bankAccount.SVCSTATUS.ToString();
                }
                else if (ccAccount != null)
                {
                    type = AccountTypeEnum.Credit;
                    accountId = ccAccount.CCACCTFROM.ACCTID;
                    status = ccAccount.SVCSTATUS.ToString();
                }
                else
                    continue;

                var account = new Account(type, accountId, bankId, accountInfo.DESC, accountInfo.PHONE, subtype,
                    status);
                result.Add(account);
            }
            return result.ToArray();
        }


        public async Task<AccountTransactions> GetTransactions(Account account, TransactionsFilter filter = null)
        {
            if (filter == null)
                filter = new TransactionsFilter(DateTime.Now.Date.AddMonths(-3), DateTime.Now.Date);
            else
               if (filter.StartDate > filter.EndDate)
                throw new Exception("Invalid Filter!!! Start date can't be greater to end date.");

            switch (account.Type)
            {
                case AccountTypeEnum.Credit:
                    return await GetCreditCardTransactions(account, filter);
                default:
                    return await GetBankAccountTransactions(account, filter);
            }
        }

        async Task<AccountTransactions> GetCreditCardTransactions(Account account, TransactionsFilter filter)
        {
            var transactionsRequest = new CreditcardRequestMessageSetV1()
            {
                Items = new AbstractTransactionRequest[]
                {
                    new CreditCardStatementTransactionRequest()
                    {
                        TRNUID = _utils.GenerateTransactionId(),
                        CCSTMTRQ = new CreditCardStatementRequest()
                        {
                            CCACCTFROM = new CreditCardAccount()
                            {
                                ACCTID = account.Id
                            },
                            INCTRAN = new IncTransaction()
                            {
                                DTEND = _utils.DateToString(filter.EndDate),
                                DTSTART = _utils.DateToString(filter.StartDate),
                                INCLUDE = BooleanType.Y
                            }
                        }
                    }
                }
            };

            var messageSet =
                await _clientHelper.ExecuteRequest<CreditcardRequestMessageSetV1, CreditcardResponseMessageSetV1>(transactionsRequest);
            var transactionsResponse =
                messageSet.Items.FirstOrDefault(_ => _ is CreditCardStatementTransactionResponse) as CreditCardStatementTransactionResponse;

            if (transactionsResponse == null)
                throw new OfxResponseException("Required response is not present in message set.");

            var transList = transactionsResponse.CCSTMTRS.BANKTRANLIST.STMTTRN != null
                ? transactionsResponse.CCSTMTRS.BANKTRANLIST.STMTTRN.Select(MapToModel).ToList()
                : new List<Transaction>();

            decimal amount;
            if (!decimal.TryParse(transactionsResponse.CCSTMTRS.LEDGERBAL.BALAMT, out amount))
                amount = 0;

            return new AccountTransactions(amount, transList);
        }

        async Task<AccountTransactions> GetBankAccountTransactions(Account account, TransactionsFilter filter)
        {
            var transactionsRequest = new BankRequestMessageSetV1()
            {
                Items = new AbstractRequest[]
                {
                    new StatementTransactionRequest()
                    {
                        TRNUID = _utils.GenerateTransactionId(),
                        STMTRQ = new StatementRequest()
                        {
                            BANKACCTFROM = new BankAccount()
                            {
                                ACCTID = account.Id,
                                BANKID = account.BankId,
                                ACCTTYPE = (AccountEnum)Enum.Parse(typeof(AccountEnum), account.Type.ToString(), true)
                            },
                            INCTRAN = new IncTransaction()
                            {
                                DTEND = _utils.DateToString(filter.EndDate),
                                DTSTART = _utils.DateToString(filter.StartDate),
                                INCLUDE = BooleanType.Y
                            }
                        }
                    }
                }
            };

            var messageSet =
                await _clientHelper.ExecuteRequest<BankRequestMessageSetV1, BankResponseMessageSetV1>(transactionsRequest);
            var transactionsResponse =
                messageSet.Items.FirstOrDefault(_ => _ is StatementTransactionResponse) as
                    StatementTransactionResponse;

            if (transactionsResponse == null)
                throw new OfxResponseException("Required response is not present in message set.");

            var transList = transactionsResponse.STMTRS.BANKTRANLIST.STMTTRN != null
                ? transactionsResponse.STMTRS.BANKTRANLIST.STMTTRN.Select(MapToModel).ToList()
                : new List<Transaction>();

            decimal amount;
            if (!decimal.TryParse(transactionsResponse.STMTRS.AVAILBAL.BALAMT, out amount))
                amount = 0;

            return new AccountTransactions(amount, transList);
        }

        static Transaction MapToModel(StatementTransaction transactionDto)
        {
            decimal amount;
            if (!decimal.TryParse(transactionDto.TRNAMT, out amount))
                throw new OfxResponseException("Amount of transaction can not be parsed. " + transactionDto.TRNAMT);

            DateTime datePosted;
            const int targetLength = 14;
            var truncatedValue = transactionDto.DTPOSTED.Length == targetLength
                ? transactionDto.DTPOSTED
                : transactionDto.DTPOSTED.Length > targetLength
                    ? transactionDto.DTPOSTED.Substring(0, targetLength)
                    : transactionDto.DTPOSTED + new string('0', targetLength - transactionDto.DTPOSTED.Length);
            if (!DateTime.TryParseExact(truncatedValue, Utils.DateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out datePosted))
                throw new OfxResponseException("Date of transaction can not be parsed. " + transactionDto.DTPOSTED);

            var description = transactionDto.Item is Payee
                ? (transactionDto.Item as Payee).NAME
                : transactionDto.Item as string;

            return new Transaction(
                transactionDto.FITID,
                transactionDto.TRNTYPE.ToString(),
                amount,
                datePosted,
                description,
                transactionDto.MEMO);
        }
    }
}
