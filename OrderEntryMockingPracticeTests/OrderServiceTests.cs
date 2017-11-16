using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using NUnit.Framework.Internal;
using OrderEntryMockingPractice.Models;
using OrderEntryMockingPractice.Services;
using Rhino.Mocks;

namespace OrderEntryMockingPracticeTests
{

    [TestFixture]
    public class OrderServiceTests
    {
        private IProductRepository _mockIProductRepository;
        private ICustomerRepository _mockICustomerRepository;
        private IEmailService _mockIEmailService;
        private IOrderFulfillmentService _mockIOrderFulfillmentService;
        private ITaxRateService _mockITaxRateService;

        [SetUp]
        public void BeforeEach()
        {
            _mockIProductRepository = MockRepository.GenerateMock<IProductRepository>();

            _mockICustomerRepository = MockRepository.GenerateMock<ICustomerRepository>();

            _mockIEmailService = MockRepository.GenerateMock<IEmailService>();

            _mockIOrderFulfillmentService = MockRepository.GenerateMock<IOrderFulfillmentService>();

            _mockITaxRateService = MockRepository.GenerateMock<ITaxRateService>();
        }

        public Order MakeOrders()
        {
            var order = new Order
            {
                CustomerId = 1,
                OrderItems = new List<OrderItem>
                {
                    new OrderItem
                    {
                        Product = new Product
                        {
                            Description = "Thing1",
                            Name = "ThingOne",
                            Price = 10.0m,
                            ProductId = 5,
                            Sku = "ABCDE"
                        },
                        Quantity = 12
                    },
                    new OrderItem
                    {
                        Product = new Product
                        {
                            Description = "Thing2",
                            Name = "ThingTwo",
                            Price = 11.0m,
                            ProductId = 6,
                            Sku = "BCDEF"
                        },
                        Quantity = 3
                    }

                }

            };
            return order;
        }


        public TaxEntry MakeTaxEntry()
        {
                var taxes = new TaxEntry
                {
                    Description = "TheTaxRate",
                    Rate = 0.10m
                };
                return taxes;
        }

        public Customer MakeCustomer()
        {
            var customer = new Customer
            {
                CustomerId = 1,
                AddressLine1 = "1918 Eighth Ave",
                AddressLine2 = "Suite 3100",
                City = "Seattle",
                Country = "USA",
                CustomerName = "Joe Money",
                EmailAddress = "name@domain.com",
                PostalCode = "98101",
                StateOrProvince = "WA"
            };
            return customer;
        }

        [Test]
        public void OrderItemsAreUniqueByProductSku()
        {
            var order = MakeOrders();

            _mockIProductRepository.Stub(a => a.IsInStock(Arg<string>.Is.Anything)).Return(true);

            _mockIOrderFulfillmentService.Stub(s => s.Fulfill(order));

            var orderService = new OrderService(_mockIProductRepository, _mockICustomerRepository, _mockIEmailService,
                _mockIOrderFulfillmentService, _mockITaxRateService);
            var result = orderService.PlaceOrder(order);

            Assert.IsNotNull(result);
        }

        [Test]
        public void OrderItemsNotUniqueByProductSkuReturnsNull()
        {
            var order = MakeOrders();
            order.OrderItems[1].Product.Sku = order.OrderItems[0].Product.Sku;

            var mockIProductRepository = MockRepository.GenerateMock<IProductRepository>();

            mockIProductRepository.Stub(a => a.IsInStock(Arg<string>.Is.Anything)).Return(false);

            var orderService = new OrderService(_mockIProductRepository, _mockICustomerRepository, _mockIEmailService,
                _mockIOrderFulfillmentService, _mockITaxRateService);
            var result = orderService.PlaceOrder(order);

            Assert.IsNull(result);
        }

        [Test]
        public void AllOrderItemsMustBeInStock()
        {
            var order = MakeOrders();

            _mockIProductRepository.Stub(a => a.IsInStock("ABCDE")).Return(true);
            _mockIProductRepository.Stub(a => a.IsInStock("BCDEF")).Return(true);

            _mockIOrderFulfillmentService.Stub(s => s.Fulfill(order));

            var orderService = new OrderService(_mockIProductRepository, _mockICustomerRepository, _mockIEmailService,
                _mockIOrderFulfillmentService, _mockITaxRateService);

            var result = orderService.PlaceOrder(order);

            Assert.IsNotNull(result);
        }

        [Test]
        public void OrderItemsNotInStockReturnNull()
        {
            var order = MakeOrders();

            var mockIProductRepository = MockRepository.GenerateMock<IProductRepository>();

            mockIProductRepository.Stub(a => a.IsInStock("ABCDE")).Return(false);

            var orderService = new OrderService(_mockIProductRepository, _mockICustomerRepository, _mockIEmailService,
                _mockIOrderFulfillmentService, _mockITaxRateService);

            var result = orderService.PlaceOrder(order);

            Assert.IsNull(result);
        }

        [Test]
        public void ValidOrder_ReturnsOrderSummary()
        {
            var order = MakeOrders();

            _mockIProductRepository.Stub(a => a.IsInStock("ABCDE")).Return(true);
            _mockIProductRepository.Stub(a => a.IsInStock("BCDEF")).Return(true);

            _mockIOrderFulfillmentService.Stub(s => s.Fulfill(order));

            var orderService = new OrderService(_mockIProductRepository, _mockICustomerRepository, _mockIEmailService,
                _mockIOrderFulfillmentService, _mockITaxRateService);

            var orderSummary = orderService.PlaceOrder(order);

            Assert.IsNotNull(orderSummary);
            _mockIOrderFulfillmentService.AssertWasCalled(ofs => ofs.Fulfill(order));
        }

        [Test]
        public void InvalidOrder_ReturnsValidationListExceptions()
        {
            //
        }

        [Test]
        public void ValidOrderSummary_ContainsOrderFulfillmentConfirmationNumber()
        {
            var order = MakeOrders();

            var orderService = new OrderService(_mockIProductRepository, _mockICustomerRepository, _mockIEmailService,
                _mockIOrderFulfillmentService, _mockITaxRateService);

            var expectedOrderNumber = "AX1123";

            _mockIProductRepository.Stub(a => a.IsInStock("ABCDE")).Return(true);
            _mockIProductRepository.Stub(a => a.IsInStock("BCDEF")).Return(true);

            _mockIOrderFulfillmentService.Stub(s => s.Fulfill(order))
                .Return(new OrderConfirmation {OrderNumber = expectedOrderNumber});

            var orderSummary = orderService.PlaceOrder(order);

            Assert.That(orderSummary.OrderNumber, Is.EqualTo(expectedOrderNumber));
        }

        [Test]
        public void ValidOrderSummary_ContainsIDGeneratedByOrderFulfillmentService()
        {
            var order = MakeOrders();

            var orderService = new OrderService(_mockIProductRepository, _mockICustomerRepository, _mockIEmailService,
                _mockIOrderFulfillmentService, _mockITaxRateService);

            var expectedIDNumber = 7;

            _mockIProductRepository.Stub(a => a.IsInStock("ABCDE")).Return(true);
            _mockIProductRepository.Stub(a => a.IsInStock("BCDEF")).Return(true);

            _mockIOrderFulfillmentService.Stub(s => s.Fulfill(order))
                .Return(new OrderConfirmation { OrderId = expectedIDNumber });

            var orderSummary = orderService.PlaceOrder(order);

            Assert.That(orderSummary.OrderId, Is.EqualTo(expectedIDNumber));

        }

        [Test]
        public void TaxesCanBeRetrievedFromTaxRateService()
        {
            var order = MakeOrders();

            var orderService = new OrderService(_mockIProductRepository, _mockICustomerRepository, _mockIEmailService,
                _mockIOrderFulfillmentService, _mockITaxRateService);

            _mockIProductRepository.Stub(a => a.IsInStock("ABCDE")).Return(true);
            _mockIProductRepository.Stub(a => a.IsInStock("BCDEF")).Return(true);

            _mockIOrderFulfillmentService.Stub(s => s.Fulfill(order));

            var expectedTaxes = new List<TaxEntry>()
            {
                new TaxEntry()
                {
                    Description = "TaxOne",
                    Rate = 0.10m,
                },
                new TaxEntry()
                {
                    Description = "TaxTwo",
                    Rate = 0.20m,
                }
            };

            _mockITaxRateService.Stub(s => s.GetTaxEntries(Arg<string>.Is.Anything, Arg<string>.Is.Anything)).Return(expectedTaxes);

            var orderSummary = orderService.PlaceOrder(order);


            Assert.That(orderSummary.Taxes, Is.Not.Null);
            Assert.That(orderSummary.Taxes, Is.EqualTo(expectedTaxes));
        }

        [Test]
        public void ValidOrder_SendsConfirmationEMailToCustomer()
        {
            var order = MakeOrders();

            var orderService = new OrderService(_mockIProductRepository, _mockICustomerRepository, _mockIEmailService,
                _mockIOrderFulfillmentService, _mockITaxRateService);

            _mockIProductRepository.Stub(a => a.IsInStock("ABCDE")).Return(true);
            _mockIProductRepository.Stub(a => a.IsInStock("BCDEF")).Return(true);

            _mockIOrderFulfillmentService.Stub(s => s.Fulfill(order));

            var orderSummary = orderService.PlaceOrder(order);

            Assert.IsNotNull(orderSummary);
            _mockIEmailService.AssertWasCalled(es => es.SendOrderConfirmationEmail(orderSummary.CustomerId,orderSummary.OrderId));
        }

        [Test]
        public void CustomerInformation_CanBePulledFromCustomerRepository()
        {
            
        }
    }
}