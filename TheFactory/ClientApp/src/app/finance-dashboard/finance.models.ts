export interface FinanceDashboardDto {
  totalBilled: number;
  totalCollected: number;
  outstandingBalance: number;
  learners: FinanceLearnerBalanceDto[];
}

export interface FinanceLearnerBalanceDto {
  learnerId: number;
  firstName: string;
  surname: string;
  grade: string;
  billed: number;
  collected: number;
  balance: number;
}

export interface FeeStructureDto {
  feeStructureId: number;
  schoolId: number;
  grade: string;
  term: string;
  academicYear: number;
  category: string;
  description: string;
  amount: number;
}

export interface FeeStructureUpsertRequest {
  grade: string;
  term: string;
  academicYear: number;
  category: string;
  description: string;
  amount: number;
}

export interface StudentLedgerEntryDto {
  transactionId: number;
  learnerId: number;
  date: string;
  type: 'DEBIT' | 'CREDIT';
  category: string;
  amount: number;
  runningBalance: number;
  notes: string | null;
  referenceNumber: string | null;
  recordedBy: number;
  isVoided: boolean;
}

export interface LedgerTransactionUpsertRequest {
  date: string;
  type: 'DEBIT' | 'CREDIT';
  category: string;
  amount: number;
  notes: string;
  referenceNumber: string;
}

export interface BulkInvoiceResultDto {
  invoiceCount: number;
  learnerCount: number;
  message: string;
}
