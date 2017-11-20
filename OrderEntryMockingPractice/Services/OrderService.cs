using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using OrderEntryMockingPractice.Models;

namespace OrderEntryMockingPractice.Services
{
    public class OrderService
    {
        private readonly IProductRepository _productRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IEmailService _emailService;
        private readonly IOrderFulfillmentService _orderFulfillmentService;
        private readonly ITaxRateService _taxRateService;

        public OrderService(IProductRepository productRepository, ICustomerRepository customerRepository,
            IEmailService emailService, IOrderFulfillmentService orderFulfillmentService,
            ITaxRateService taxRateService)
        {
            _productRepository = productRepository;
            _customerRepository = customerRepository;
            _emailService = emailService;
            _orderFulfillmentService = orderFulfillmentService;
            _taxRateService = taxRateService;
        }
        
        public OrderSummary PlaceOrder(Order order)
        {
            if (!ValidateOrder(order))
                return null;
   
            var confirmation =  _orderFulfillmentService.Fulfill(order);

            var customer = _customerRepository.Get(confirmation.CustomerId);

            var taxes = _taxRateService.GetTaxEntries(customer.PostalCode,customer.Country);

            var netTotal = order.OrderItems.Sum(c => c.Quantity * c.Product.Price);

            var orderSummary = new OrderSummary
            {
                CustomerId = confirmation.CustomerId,
                OrderNumber = confirmation.OrderNumber,
                OrderId = confirmation.OrderId,
                NetTotal = netTotal,
                Taxes = taxes,
            };

            _emailService.SendOrderConfirmationEmail(orderSummary.CustomerId,orderSummary.OrderId);

            return orderSummary;
        }

        private bool ValidateOrder(Order order)
        {
            var skus = order.OrderItems.Select(x => x.Product.Sku);

            var skusList = skus as IList<string> ?? skus.ToList();
            return SkusAreUnique(skusList) && skusList.All(sku => _productRepository.IsInStock(sku));
        }

        private static bool SkusAreUnique(IEnumerable<string> skus)
        {
            var skuList = skus as IList<string> ?? skus.ToList();
            return skuList.Distinct().Count() == skuList.Count;
        }
    }
}