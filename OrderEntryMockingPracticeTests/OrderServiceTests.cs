﻿using System.Collections.Generic;
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
                            Description = "Product",
                            Name = "Name of Product.",
                            Price = 10.0m,
                            ProductId = 5,
                            Sku = "ABCDE"
                        },
                        Quantity = 10
                    },
                    new OrderItem
                    {
                        Product = new Product
                        {
                            Description = "Product",
                            Name = "Name of Product.",
                            Price = 10.0m,
                            ProductId = 5,
                            Sku = "BCDEF"
                        },
                        Quantity = 10
                    }

                }

            };
            return order;
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
    }
}