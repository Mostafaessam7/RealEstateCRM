export const PaymentStatus = {
  Pending: "Pending",
  Paid: "Paid",
  Failed: "Failed",
  Cancelled: "Cancelled",
} as const;
export type PaymentStatus = (typeof PaymentStatus)[keyof typeof PaymentStatus];

export interface Payment {
  id: string;
  dealId: string;
  amount: number;
  currency: string;
  status: PaymentStatus;
  paidAt: string | null;
  createdAt: string;
}

export interface CreateCheckoutRequest {
  amount?: number;
  currency?: string;
}

export interface CheckoutSession {
  paymentId: string;
  checkoutUrl: string;
}
