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
            var skus = order.OrderItems.Select(x => x.Product.Sku);
            
            if (!StringsAreUnique(skus))
                //add exception to list
                 return null;
            foreach (var sku in skus)
                if (!_productRepository.IsInStock(sku))
                    //add exception to list
                    return null;
   
            var confirmation =  _orderFulfillmentService.Fulfill(order);


            var orderSummary = new OrderSummary
            {
                CustomerId = confirmation.CustomerId,
                OrderNumber = confirmation.OrderNumber,
                OrderId = confirmation.OrderId,
                Taxes = _taxRateService.GetTaxEntries("postalCode","country"),
            };

            _emailService.SendOrderConfirmationEmail(orderSummary.CustomerId,orderSummary.OrderId);

            return orderSummary;
        }

        private bool StringsAreUnique(IEnumerable<string> strings)
        {
            return strings.Distinct().Count() == strings.Count();
        }
    }
}