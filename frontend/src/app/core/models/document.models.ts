export interface DocumentUploadResponse {
  documentId: number;
  message: string;
}

export interface DocumentListItem {
  documentId: number;
  fileName: string;
  status: string;
  documentType: string;
  fileSize: number;
  createdAt: string;
  processedAt?: string | null;
}

export interface DocumentExtractionResponse {
  documentId: number;
  fileName: string;
  documentType: string;
  extractedData: string;
  status: string;
  fileSize: number;
  createdAt: string;
  processedAt?: string | null;
}

export interface ExtractedLineItem {
  description?: string | null;
  quantity?: number | string | null;
  unitPrice?: number | string | null;
  amount?: number | string | null;
  [key: string]: unknown;
}

export interface ExtractedInvoiceData {
  documentType?: string;
  invoiceNumber?: string | null;
  invoiceDate?: string | null;
  vendor?: string | null;
  customer?: string | null;
  totalAmount?: number | string | null;
  currency?: string | null;
  lineItems?: ExtractedLineItem[];
  [key: string]: unknown;
}
