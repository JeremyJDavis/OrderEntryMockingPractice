using System;
using System.Collections.Generic;
using System.Linq;
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
            ValidateOrder(order);
   
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

        private void ValidateOrder(Order order)
        {
            var skus = order.OrderItems.Select(x => x.Product.Sku);

            var exceptionsList = new List<string>();

            var skusList = skus as IList<string> ?? skus.ToList();
            if (!SkusAreUnique(skusList))
            {
                 exceptionsList.Add("Duplicate SKUs found.");
            }

            if (skusList.Any(sku => !_productRepository.IsInStock(sku)))
            {
                exceptionsList.Add("Ordered item(s) not in stock.");
            }

            if (exceptionsList.Any())
            {
                throw new OrderServiceException(exceptionsList);
            }
        }

        private static bool SkusAreUnique(IEnumerable<string> skus)
        {
            var skuList = skus as IList<string> ?? skus.ToList();
            return skuList.Distinct().Count() == skuList.Count;
        }
    }

    public class OrderServiceException : Exception
    {
        public List<string> ExceptionList { get; }

        public OrderServiceException(List<string> exceptionList)
        {
            ExceptionList = exceptionList;
        }

        public override string Message
        {
            get
            {
                var message = "";

                foreach (var reason in ExceptionList)
                {
                    message += $"{reason} ";
                }
                return message;
            }
        }
    }
}