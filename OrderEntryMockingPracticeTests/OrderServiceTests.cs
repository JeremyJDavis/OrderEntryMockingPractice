using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
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

        private static Order MakeOrders()
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
        
        [Test]
        public void OrderItemsAreUniqueByProductSku()
        {
            var order = MakeOrders();

            var orderService = new OrderService(_mockIProductRepository, _mockICustomerRepository, _mockIEmailService,
                _mockIOrderFulfillmentService, _mockITaxRateService);

            _mockIProductRepository.Stub(a => a.IsInStock(Arg<string>.Is.Anything)).Return(true);

            var orderConfirmation = new OrderConfirmation { OrderNumber = "AX1123", CustomerId = 1, OrderId = 7 };
            _mockIOrderFulfillmentService.Stub(s => s.Fulfill(order))
                .Return(orderConfirmation);

            var customer = new Customer
            {
                CustomerId = 1,
                PostalCode = "99999",
                StateOrProvince = "WA"
            };

            _mockICustomerRepository.Stub(c => c.Get(Arg<int>.Is.Anything)).Return(customer);

            var result = orderService.PlaceOrder(order);
            
            Assert.IsNotNull(result);
        }

        [Test]
        public void AllOrderItemsMustBeInStock()
        {
            var order = MakeOrders();

            _mockIProductRepository.Stub(a => a.IsInStock("ABCDE")).Return(true);
            _mockIProductRepository.Stub(a => a.IsInStock("BCDEF")).Return(true);

            _mockIOrderFulfillmentService.Stub(s => s.Fulfill(order))
                .Return(new OrderConfirmation { OrderNumber = "AX1123", CustomerId = 1, OrderId = 7 });

            var customer = new Customer
            {
                CustomerId = 1,
                PostalCode = "99999",
                StateOrProvince = "WA"
            };

            _mockICustomerRepository.Stub(c => c.Get(Arg<int>.Is.Anything)).Return(customer);

            var orderService = new OrderService(_mockIProductRepository, _mockICustomerRepository, _mockIEmailService,
                _mockIOrderFulfillmentService, _mockITaxRateService);

            var result = orderService.PlaceOrder(order);

            Assert.IsNotNull(result);
        }

        [Test]
        public void ValidOrder_ReturnsOrderSummary()
        {
            var order = MakeOrders();

            _mockIProductRepository.Stub(a => a.IsInStock("ABCDE")).Return(true);
            _mockIProductRepository.Stub(a => a.IsInStock("BCDEF")).Return(true);

            _mockIOrderFulfillmentService.Stub(s => s.Fulfill(order))
                .Return(new OrderConfirmation { OrderNumber = "AX1123", CustomerId = 1, OrderId = 7 });

            var orderService = new OrderService(_mockIProductRepository, _mockICustomerRepository, _mockIEmailService,
                _mockIOrderFulfillmentService, _mockITaxRateService);

            var customer = new Customer
            {
                CustomerId = 1,
                PostalCode = "99999",
                StateOrProvince = "WA"
            };

            _mockICustomerRepository.Stub(c => c.Get(Arg<int>.Is.Anything)).Return(customer);

            var orderSummary = orderService.PlaceOrder(order);

            Assert.IsNotNull(orderSummary);
            _mockIOrderFulfillmentService.AssertWasCalled(ofs => ofs.Fulfill(order));
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
                .Return(new OrderConfirmation { OrderNumber = expectedOrderNumber, CustomerId = 1, OrderId = 7 });

            var customer = new Customer
            {
                CustomerId = 1,
                PostalCode = "99999",
                StateOrProvince = "WA"
            };

            _mockICustomerRepository.Stub(c => c.Get(Arg<int>.Is.Anything)).Return(customer);

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
                .Return(new OrderConfirmation { OrderNumber = "AX1123", CustomerId = 1, OrderId = expectedIDNumber });

            var customer = new Customer
            {
                CustomerId = 1,
                PostalCode = "99999",
                StateOrProvince = "WA"
            };

            _mockICustomerRepository.Stub(c => c.Get(Arg<int>.Is.Anything)).Return(customer);

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

            _mockIOrderFulfillmentService.Stub(s => s.Fulfill(order))
                .Return(new OrderConfirmation { OrderNumber = "AX1123", CustomerId = 1, OrderId = 7 });

            var customer = new Customer
            {
                CustomerId = 1,
                PostalCode = "99999",
                StateOrProvince = "WA"
            };

            _mockICustomerRepository.Stub(c => c.Get(Arg<int>.Is.Anything)).Return(customer);

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

            _mockIOrderFulfillmentService.Stub(s => s.Fulfill(order))
                .Return(new OrderConfirmation { OrderNumber = "AX1123", CustomerId = 1, OrderId = 7 });

            var customer = new Customer
            {
                CustomerId = 1,
                PostalCode = "99999",
                StateOrProvince = "WA"
            };

            _mockICustomerRepository.Stub(c => c.Get(Arg<int>.Is.Anything)).Return(customer);

            var orderSummary = orderService.PlaceOrder(order);

            Assert.IsNotNull(orderSummary);
            _mockIEmailService.AssertWasCalled(es => es.SendOrderConfirmationEmail(orderSummary.CustomerId,orderSummary.OrderId));
        }

        [Test]
        public void CustomerInformation_CanBePulledFromCustomerRepository()
        {
            

            var order = MakeOrders();

            var orderService = new OrderService(_mockIProductRepository, _mockICustomerRepository, _mockIEmailService,
                _mockIOrderFulfillmentService, _mockITaxRateService);

            _mockIProductRepository.Stub(a => a.IsInStock("ABCDE")).Return(true);
            _mockIProductRepository.Stub(a => a.IsInStock("BCDEF")).Return(true);

            _mockIOrderFulfillmentService.Stub(s => s.Fulfill(order))
                .Return(new OrderConfirmation { OrderNumber = "AX1123", CustomerId = 1, OrderId = 7 });

            var customer = new Customer
            {
                CustomerId = 1,
                PostalCode = "99999",
                StateOrProvince = "WA"
            };

            _mockICustomerRepository.Stub(c => c.Get(Arg<int>.Is.Anything)).Return(customer);

            var orderSummary = orderService.PlaceOrder(order);

            var expectedCustomerId = 1;
            
            Assert.That(orderSummary.CustomerId, Is.Not.Null);
            Assert.That(orderSummary.CustomerId, Is.EqualTo(expectedCustomerId));
        }

        [Test]
        public void ValidOrderSummary_ContainsCorrectNetTotalForCustomer()
        {
            var order = MakeOrders();

            var expectedTaxes = new List<TaxEntry>()
            {
                new TaxEntry()
                {
                    Description = "TaxOne",
                    Rate = 0.10m,
                }
            };

            var orderService = new OrderService(_mockIProductRepository, _mockICustomerRepository, _mockIEmailService,
                _mockIOrderFulfillmentService, _mockITaxRateService);

            _mockIProductRepository.Stub(a => a.IsInStock("ABCDE")).Return(true);
            _mockIProductRepository.Stub(a => a.IsInStock("BCDEF")).Return(true);

            _mockIOrderFulfillmentService.Stub(s => s.Fulfill(order))
                .Return(new OrderConfirmation { OrderNumber = "AX1123", CustomerId = 1, OrderId = 7 });

            var customer = new Customer
            {
                CustomerId = 1,
                PostalCode = "99999",
                StateOrProvince = "WA"
            };

            _mockICustomerRepository.Stub(c => c.Get(Arg<int>.Is.Anything)).Return(customer);

            _mockITaxRateService.Stub(s => s.GetTaxEntries(Arg<string>.Is.Anything, Arg<string>.Is.Anything))
                .Return(expectedTaxes);

            var orderSummary = orderService.PlaceOrder(order);

            var expectedNetTotal = order.OrderItems.Sum(i => i.Quantity * i.Product.Price);

            Assert.That(orderSummary.NetTotal, Is.EqualTo(expectedNetTotal));
        }

        [Test]
        public void ValidOrderSummary_GetsTaxesForCustomer()
        {
            var order = MakeOrders();

            var expectedTaxes = new List<TaxEntry>()
            {
                new TaxEntry()
                {
                    Description = "TaxOne",
                    Rate = 0.10m,
                }
            };

            var orderService = new OrderService(_mockIProductRepository, _mockICustomerRepository, _mockIEmailService,
                _mockIOrderFulfillmentService, _mockITaxRateService);

            _mockIProductRepository.Stub(a => a.IsInStock("ABCDE")).Return(true);
            _mockIProductRepository.Stub(a => a.IsInStock("BCDEF")).Return(true);

            _mockIOrderFulfillmentService.Stub(s => s.Fulfill(order))
                .Return(new OrderConfirmation { OrderNumber = "AX1123", CustomerId = 1, OrderId = 7 });

            var customer = new Customer
            {
                CustomerId = 1,
                PostalCode = "99999",
                StateOrProvince = "WA"
            };

            _mockICustomerRepository.Stub(c => c.Get(Arg<int>.Is.Anything)).Return(customer);

            _mockITaxRateService.Stub(s => s.GetTaxEntries(Arg<string>.Is.Anything, Arg<string>.Is.Anything))
                .Return(expectedTaxes);

            var orderSummary = orderService.PlaceOrder(order);

            Assert.That(orderSummary.Taxes, Is.EqualTo(expectedTaxes));
        }
        
        [Test]
        public void InvalidOrder_ThrowsValidationExeptionForDuplicateSkus()
        {
            var order = MakeOrders();

            order.OrderItems[1].Product.Sku = order.OrderItems[0].Product.Sku;

            var mockIProductRepository = MockRepository.GenerateMock<IProductRepository>();

            mockIProductRepository.Stub(a => a.IsInStock(Arg<string>.Is.Anything)).Return(false);

            var orderService = new OrderService(_mockIProductRepository, _mockICustomerRepository, _mockIEmailService,
                _mockIOrderFulfillmentService, _mockITaxRateService);

            try
            {
                orderService.PlaceOrder(order);
                Assert.Fail("Expected an Exception to be thrown.");
            }
            catch (Exception e)
            {
                Assert.That(e, Is.InstanceOf<OrderServiceException>());
                var exception = e as OrderServiceException;
                Assert.That(exception.ExceptionList, Contains.Item("Duplicate SKUs found."));
            }
        }

        [Test]
        public void InvalidOrder_ThrowsValidationListForProductNotInStock()
        {
            var order = MakeOrders();

            var mockIProductRepository = MockRepository.GenerateMock<IProductRepository>();

            mockIProductRepository.Stub(a => a.IsInStock("ABCDE")).Return(false);

            var orderService = new OrderService(_mockIProductRepository, _mockICustomerRepository, _mockIEmailService,
                _mockIOrderFulfillmentService, _mockITaxRateService);


            try
            {
                orderService.PlaceOrder(order);
                Assert.Fail("Expected an Exception to be thrown.");
            }
            catch (Exception e)
            {
                Assert.That(e, Is.InstanceOf<OrderServiceException>());
                var exception = e as OrderServiceException;
                Assert.That(exception.ExceptionList, Contains.Item("Ordered item(s) not in stock."));
            }
        }


        [Test]
        public void InvalidOrder_ThrowsValidationListForProductNotInStockAndDuplicateSkus()
        {
            var order = MakeOrders();

            order.OrderItems[1].Product.Sku = order.OrderItems[0].Product.Sku;
            var mockIProductRepository = MockRepository.GenerateMock<IProductRepository>();

            mockIProductRepository.Stub(a => a.IsInStock("ABCDE")).Return(false);

            var orderService = new OrderService(_mockIProductRepository, _mockICustomerRepository, _mockIEmailService,
                _mockIOrderFulfillmentService, _mockITaxRateService);

            try

            {
                orderService.PlaceOrder(order);
                Assert.Fail("Expected an Exception to be thrown.");
            }
            catch (Exception e)
            {
                Assert.That(e, Is.InstanceOf<OrderServiceException>());
                var exception = e as OrderServiceException;
                Assert.That(exception.ExceptionList, Contains.Item("Ordered item(s) not in stock."));
                Assert.That(exception.ExceptionList, Contains.Item("Duplicate SKUs found."));
            }
        }

    }
}