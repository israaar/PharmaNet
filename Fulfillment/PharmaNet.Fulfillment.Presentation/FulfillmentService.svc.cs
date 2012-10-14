﻿using System;
using System.Collections.Generic;
using System.Linq;
using PharmaNet.Fulfillment.Application;
using PharmaNet.Fulfillment.Contract;
using PharmaNet.Fulfillment.Domain;
using PharmaNet.Fulfillment.SQL;
using System.Transactions;
using System.ServiceModel;

namespace PharmaNet.Fulfillment.Presentation
{
    public class FulfillmentService : IFulfillmentService
    {
        private CustomerService _customerService;
        private ProductService _productService;
        private InventoryAllocationService _inventoryAllocationService;
        private PickListService _pickListService;

        private IMessageQueue<Order> _messageQueue;

        private Random _networkError = new Random();

        public FulfillmentService()
        {
            FulfillmentDB.Initialize();

            FulfillmentDB context = new FulfillmentDB();

            _customerService = new CustomerService(
                context.GetCustomerRepository());
            _productService = new ProductService(
                context.GetProductRepository());
            _inventoryAllocationService = new InventoryAllocationService(
                context.GetWarehouseRepository());
            _pickListService = new PickListService(
                context.GetPickListRepository());

            _messageQueue = MsmqMessageQueue<Order>
                .Instance;
        }

        public void PlaceOrder(Order order)
        {
            _messageQueue.Send(order);

            if (_networkError.Next(100) < 20)
                throw new FaultException<FulfillmentNetworkError>(
                    new FulfillmentNetworkError(),
                    "Network error");
        }

        public Confirmation CheckOrderStatus(Guid orderId)
        {
            var pickLists = _pickListService
                .GetPickLists(orderId);

            if (!pickLists.Any())
                return null;

            return new Confirmation
            {
                Shipments = pickLists
                    .Select(pickList => new Shipment
                    {
                        ProductId = pickList.Product
                            .ProductId,
                        Quantity = pickList.Quantity,
                        TrackingNumber = "123-456"
                    })
                    .ToList()
            };
        }
    }
}
