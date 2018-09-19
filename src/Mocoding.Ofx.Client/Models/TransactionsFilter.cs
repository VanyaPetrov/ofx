using System;

namespace Mocoding.Ofx.Client.Models
{
    public class TransactionsFilter
    {
        public TransactionsFilter(DateTime startDate, DateTime endDate)
        {
            StartDate = startDate;
            EndDate = endDate;
        }

        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }
    }
}