﻿namespace Miki.Services.Transactions
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using Miki.Bot.Models;
    using Miki.Bot.Models.Exceptions;
    using Miki.Functional;

    public class TransactionService : ITransactionService
    {
        public Func<TransactionResponse, Task> TransactionComplete { get; set; }
        public Func<TransactionRequest, Exception, Task> TransactionFailed { get; set; }

        private readonly TransactionEvents events;
        private readonly IUserService service;

        public TransactionService(IUserService service, [Optional] TransactionEvents events)
        {
            this.events = events;
            this.service = service;
        }

        public async Task<TransactionResponse> CreateTransactionAsync(TransactionRequest transaction)
        {
            var receiver = await service.GetUserAsync(transaction.Receiver);
            var sender = await service.GetUserAsync(transaction.Sender);

            try
            {
                var response = await TransferAsync(receiver, sender, transaction.Amount);
                await CommitAsync();
                if(events != null)
                {
                    await events.CallTransactionCompleted(response);
                }
                return response;
            }
            catch(Exception e)
            {
                if(events != null)
                {
                    await events.CallTransactionFailed(transaction, e);
                }
                throw;
            }
        }

        private ValueTask CommitAsync()
        {
            return service.SaveAsync();
        }

        private async Task<TransactionResponse> TransferAsync(User receiver, User sender, long amount)
        {
            if(amount <= 0)
            {
                throw new ArgumentLessThanZeroException();
            }

            if(sender.Currency < amount)
            {
                throw new InsufficientCurrencyException(sender.Currency, amount);
            }

            if(receiver.Id == sender.Id)
            {
                throw new UserNullException();
            }

            sender.Currency -= (int)amount;
            await service.UpdateUserAsync(sender);

            receiver.Currency += (int)amount;
            await service.UpdateUserAsync(receiver);

            return new TransactionResponse(sender, receiver, amount);
        }
    }

    public class TransactionResponse
    {
        public User Sender { get; }
        public User Receiver { get; }
        public long Amount { get; }

        public TransactionResponse(User sender, User receiver, long amount)
        {
            Sender = sender;
            Receiver = receiver;
            Amount = amount;
        }

    }

    public class TransactionRequest
    {
        public long Sender { get; }
        public long Receiver { get; }
        public long Amount { get; }

        public TransactionRequest(long sender, long receiver, long amount)
        {
            this.Sender = sender;
            this.Receiver = receiver;
            this.Amount = amount;
        }

        public class Builder
        {
            private Optional<long> sender;
            private Optional<long> receiver;
            private Optional<long> amount;

            public Builder()
            {
                sender = Optional<long>.None;
                receiver = Optional<long>.None;
                amount = Optional<long>.None;
            }
            public TransactionRequest Build()
            {
                if(!sender.HasValue)
                {
                    throw new FailedTransactionException(
                        new ArgumentNullException(nameof(sender)));
                }

                if(!receiver.HasValue)
                {
                    throw new FailedTransactionException(
                        new ArgumentNullException(nameof(receiver)));
                }

                if(!amount.HasValue)
                {
                    throw new FailedTransactionException(
                        new InvalidOperationException("amount cannot be zero"));
                }
                if(amount < 0L)
                {
                    throw new FailedTransactionException(
                        new ArgumentLessThanZeroException());
                }

                return new TransactionRequest(sender, receiver, amount);
            }

            public Builder WithAmount(long amount)
            {
                this.amount = amount;
                return this;
            }

            public Builder WithReceiver(long receiver)
            {
                this.receiver = receiver;
                return this;
            }

            public Builder WithSender(long sender)
            {
                this.sender = sender;
                return this;
            }
        }
    }
}
