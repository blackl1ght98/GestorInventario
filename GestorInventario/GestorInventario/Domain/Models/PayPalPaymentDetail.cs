﻿using System;
using System.Collections.Generic;

namespace GestorInventario.Domain.Models;

public partial class PayPalPaymentDetail
{
    public string Id { get; set; } = null!;

    public string? Intent { get; set; }

    public string? State { get; set; }

    public string? Cart { get; set; }

    public string? PaymentMethod { get; set; }

    public string? PayerStatus { get; set; }

    public string? PayerEmail { get; set; }

    public string? PayerFirstName { get; set; }

    public string? PayerLastName { get; set; }

    public string? PayerId { get; set; }

    public string? ShippingRecipientName { get; set; }

    public string? ShippingLine1 { get; set; }

    public string? ShippingCity { get; set; }

    public string? ShippingState { get; set; }

    public string? ShippingPostalCode { get; set; }

    public string? ShippingCountryCode { get; set; }

    public decimal? TransactionsTotal { get; set; }

    public string? TransactionsCurrency { get; set; }

    public decimal? TransactionsSubtotal { get; set; }

    public decimal? TransactionsShipping { get; set; }

    public decimal? TransactionsInsurance { get; set; }

    public decimal? TransactionsHandlingFee { get; set; }

    public decimal? TransactionsShippingDiscount { get; set; }

    public decimal? TransactionsDiscount { get; set; }

    public string? PayeeMerchantId { get; set; }

    public string? PayeeEmail { get; set; }

    public string? Description { get; set; }

    public string? SaleId { get; set; }

    public string? SaleState { get; set; }

    public decimal? SaleTotal { get; set; }

    public string? SaleCurrency { get; set; }

    public decimal? SaleSubtotal { get; set; }

    public decimal? SaleShipping { get; set; }

    public decimal? SaleInsurance { get; set; }

    public decimal? SaleHandlingFee { get; set; }

    public decimal? SaleShippingDiscount { get; set; }

    public decimal? SaleDiscount { get; set; }

    public string? PaymentMode { get; set; }

    public string? ProtectionEligibility { get; set; }

    public string? ProtectionEligibilityType { get; set; }

    public decimal? TransactionFeeAmount { get; set; }

    public string? TransactionFeeCurrency { get; set; }

    public decimal? ReceivableAmount { get; set; }

    public string? ReceivableCurrency { get; set; }

    public decimal? ExchangeRate { get; set; }

    public string? ParentPayment { get; set; }

    public DateTime? CreateTime { get; set; }

    public DateTime? UpdateTime { get; set; }

    public virtual ICollection<PayPalPaymentItem> PayPalPaymentItems { get; set; } = new List<PayPalPaymentItem>();
}