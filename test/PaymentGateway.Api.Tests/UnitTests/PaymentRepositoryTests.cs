using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;

using PaymentGateway.Api.Models.Domain;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests.UnitTests
{
    public class PaymentRepositoryTests
    {
        [Fact]
        public async Task WhenAddCalledMultipleTimesWithSameId_StoresOnlySinglePayment()
        {
            // Arrange
            var repository = new PaymentsRepository(new NullLogger<PaymentsRepository>());
            var paymentId = Guid.NewGuid();
            var payment = Payment.Create(paymentId,"2222405343248877", 4, 2026, "GBP", 100, "123", DateTime.UtcNow );
            var payment2 = Payment.Create(paymentId, "2222405343248877", 4, 2026, "GBP", 100, "123", DateTime.UtcNow.AddMilliseconds(10));
            var payment3 = Payment.Create(paymentId, "2222405343248877", 4, 2026, "GBP", 100, "123", DateTime.UtcNow.AddMilliseconds(15));

            // Act
            var first = await repository.Add(payment);
            var second = await repository.Add(payment2);
            var third = await repository.Add(payment3);

            // Assert
            var retrieved = await repository.Get(payment.Id);
            Assert.Same(first, retrieved);
            Assert.Same(first, second);
            Assert.Same(first, third);
        }

    }
}
