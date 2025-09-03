using System.Net;
using System.Net.Http.Json;
using Berca_Backend.DTOs;
using Berca_Backend.Models;
using FluentAssertions;
using Xunit;

namespace Berca_Backend.Tests;

public class FactureEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public FactureEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Facture_FullLifecycle_Should_Succeed()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // 0) Create a supplier to use in facture flows
        var createSupplier = new CreateSupplierDto
        {
            CompanyName = "Test Supplier E2E",
            ContactPerson = "QA Bot",
            Phone = "081234567890",
            Email = $"qa{Guid.NewGuid():N}@test.local".Substring(0,20)+"@t.local",
            Address = "Jl. Test No. 1",
            PaymentTerms = 30,
            CreditLimit = 100000000
        };
        var createSuppResp = await client.PostAsJsonAsync("/api/Supplier", createSupplier);
        createSuppResp.EnsureSuccessStatusCode();
        var createdSupplier = await createSuppResp.Content.ReadFromJsonAsync<ApiResponse<SupplierDto>>();
        createdSupplier.Should().NotBeNull();
        createdSupplier!.Success.Should().BeTrue();
        var supplierId = createdSupplier!.Data!.Id;

        // 1) Receive a new facture
        var receive = new ReceiveFactureDto
        {
            SupplierInvoiceNumber = $"INV-{Guid.NewGuid():N}".Substring(0, 12),
            SupplierId = supplierId,
            InvoiceDate = DateTime.UtcNow.Date,
            TotalAmount = 200000m,
            Tax = 10m,
            Discount = 0m,
            Notes = "Test facture",
            Items = new List<CreateFactureItemDto>()
        };

        var receiveResp = await client.PostAsJsonAsync("/api/Facture/receive", receive);
        var receiveBody = await receiveResp.Content.ReadAsStringAsync();
        receiveResp.StatusCode.Should().Be(HttpStatusCode.Created, receiveBody);
        var received = await receiveResp.Content.ReadFromJsonAsync<FactureDto>();
        received.Should().NotBeNull();
        received!.Id.Should().BeGreaterThan(0);

        var factureId = received.Id;

        // 2) Get facture by id
        var getResp = await client.GetAsync($"/api/Facture/{factureId}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3) Verify facture items
        var verify = new VerifyFactureDto
        {
            FactureId = factureId,
            VerificationNotes = "All good",
            Items = new List<VerifyFactureItemDto>()
        };

        // If items exist, add a verification entry for the first
        if (received.Items.Any())
        {
            verify.Items.Add(new VerifyFactureItemDto
            {
                ItemId = received.Items.First().Id,
                ReceivedQuantity = 2,
                AcceptedQuantity = 2,
                VerificationNotes = "OK",
                IsVerified = true
            });
        }

        var verifyResp = await client.PostAsJsonAsync($"/api/Facture/{factureId}/verify", verify);
        verifyResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4) Approve facture
        var approveResp = await client.PostAsJsonAsync($"/api/Facture/{factureId}/approve", "Approved in test");
        approveResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 5) Schedule a payment
        var schedule = new SchedulePaymentDto
        {
            FactureId = factureId,
            PaymentDate = DateTime.UtcNow.Date,
            Amount = 50000m,
            PaymentMethod = PaymentMethod.BankTransfer,
            Notes = "Test schedule"
        };
        var scheduleResp = await client.PostAsJsonAsync($"/api/Facture/{factureId}/payments/schedule", schedule);
        scheduleResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var scheduled = await scheduleResp.Content.ReadFromJsonAsync<FacturePaymentDto>();
        scheduled.Should().NotBeNull();

        // 6) Process the payment
        var process = new ProcessPaymentDto
        {
            PaymentId = scheduled!.Id,
            TransferReference = "TRX-TEST",
            ProcessingNotes = "Processed by test"
        };
        var processResp = await client.PostAsJsonAsync($"/api/Facture/payments/{scheduled.Id}/process", process);
        processResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 7) Confirm the payment
        var confirm = new ConfirmPaymentDto
        {
            PaymentId = scheduled.Id,
            SupplierAckReference = "SUP-ACK-001",
            ConfirmationNotes = "Confirmed by supplier"
        };
        var confirmResp = await client.PostAsJsonAsync($"/api/Facture/payments/{scheduled.Id}/confirm", confirm);
        confirmResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 8) Get payments for the facture
        var listPaymentsResp = await client.GetAsync($"/api/Facture/{factureId}/payments");
        listPaymentsResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 9) Summary and analytics endpoints
        var summaryResp = await client.GetAsync("/api/Facture/summary");
        summaryResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var analyticsResp = await client.GetAsync("/api/Facture/payment-analytics");
        analyticsResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var agingResp = await client.GetAsync("/api/Facture/aging-analysis");
        agingResp.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // 10) Outstanding factures
        var outstandingResp = await client.GetAsync("/api/Facture/outstanding-factures");
        outstandingResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
