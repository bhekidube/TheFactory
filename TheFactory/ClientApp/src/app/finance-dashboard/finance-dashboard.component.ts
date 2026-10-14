import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FinanceApiService } from '../services/finance-api.service';
import { TableSortController } from '../table-sorting';
import {
  FeeStructureDto,
  FeeStructureUpsertRequest,
  FinanceDashboardDto,
  FinanceLearnerBalanceDto,
  LedgerTransactionUpsertRequest,
  StudentLedgerEntryDto
} from './finance.models';

type FinanceFormMode = 'payment' | 'edit' | 'ad-hoc-invoice' | 'fee' | 'invoice' | null;

@Component({
  selector: 'app-finance-dashboard',
  templateUrl: './finance-dashboard.component.html',
  styleUrls: ['./finance-dashboard.component.css']
})
export class FinanceDashboardComponent implements OnInit {
  readonly tableSort = new TableSortController();
  schoolId = 0;
  dashboard: FinanceDashboardDto = {
    totalBilled: 0,
    totalCollected: 0,
    outstandingBalance: 0,
    learners: []
  };
  feeStructures: FeeStructureDto[] = [];
  ledger: StudentLedgerEntryDto[] = [];
  selectedLearner: FinanceLearnerBalanceDto | null = null;
  formMode: FinanceFormMode = null;
  editingTransaction: StudentLedgerEntryDto | null = null;
  loading = false;
  loadingLedger = false;
  saving = false;
  errorMessage = '';
  successMessage = '';

  paymentForm = this.emptyTransactionForm();
  feeForm = this.emptyFeeForm();
  selectedFeeStructureId = 0;
  selectedFeeStructure: FeeStructureDto | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly financeApi: FinanceApiService
  ) {}

  ngOnInit(): void {
    this.schoolId = Number(this.route.parent?.snapshot.paramMap.get('schoolId')) || 0;
    if (this.schoolId <= 0) {
      this.errorMessage = 'A valid school is required to open Finance.';
      return;
    }

    this.loadDashboard();
    this.loadFeeStructures();
  }

  loadDashboard(): void {
    this.loading = true;
    this.errorMessage = '';
    this.financeApi.getDashboard(this.schoolId).subscribe({
      next: dashboard => {
        this.dashboard = dashboard;
        this.loading = false;
        if (this.selectedLearner) {
          this.selectedLearner = dashboard.learners.find(
            learner => learner.learnerId === this.selectedLearner?.learnerId
          ) || null;
          if (!this.selectedLearner) {
            this.ledger = [];
          }
        }
        if (!this.selectedLearner && dashboard.learners.length > 0) {
          this.selectLearner(dashboard.learners[0]);
        } else if (this.selectedLearner) {
          this.loadLedger();
        }
      },
      error: error => {
        this.loading = false;
        this.errorMessage = this.getErrorMessage(error, 'Unable to load school finance.');
      }
    });
  }

  loadFeeStructures(): void {
    this.financeApi.getFeeStructures(this.schoolId).subscribe({
      next: feeStructures => {
        this.feeStructures = feeStructures;
      },
      error: error => {
        this.errorMessage = this.getErrorMessage(error, 'Unable to load fee structures.');
      }
    });
  }

  selectLearner(learner: FinanceLearnerBalanceDto): void {
    this.selectedLearner = learner;
    this.loadLedger();
  }

  loadLedger(): void {
    if (!this.selectedLearner) {
      this.ledger = [];
      return;
    }

    this.loadingLedger = true;
    this.financeApi.getLedger(this.schoolId, this.selectedLearner.learnerId).subscribe({
      next: ledger => {
        this.ledger = ledger;
        this.loadingLedger = false;
      },
      error: error => {
        this.loadingLedger = false;
        this.errorMessage = this.getErrorMessage(error, 'Unable to load this learner ledger.');
      }
    });
  }

  openPaymentForm(): void {
    if (!this.selectedLearner) {
      this.errorMessage = 'Select a learner before recording a payment.';
      return;
    }
    this.errorMessage = '';
    this.successMessage = '';
    this.editingTransaction = null;
    this.paymentForm = this.emptyTransactionForm();
    this.paymentForm.date = this.getToday();
    this.formMode = 'payment';
  }

  openAdHocInvoiceForm(): void {
    if (!this.selectedLearner) {
      this.errorMessage = 'Select a learner before adding an invoice.';
      return;
    }

    this.errorMessage = '';
    this.successMessage = '';
    this.editingTransaction = null;
    this.selectedFeeStructure = null;
    this.paymentForm = {
      date: this.getToday(),
      type: 'DEBIT',
      category: 'Ad-hoc Invoice',
      amount: 0,
      notes: '',
      referenceNumber: ''
    };
    this.formMode = 'ad-hoc-invoice';
  }

  onAdHocFeeStructureChange(feeStructure: FeeStructureDto | null): void {
    this.selectedFeeStructure = feeStructure;
    this.paymentForm.notes = feeStructure?.description ?? '';
    this.paymentForm.amount = feeStructure?.amount ?? 0;
  }

  openEditForm(entry: StudentLedgerEntryDto): void {
    if (entry.isVoided) {
      return;
    }
    this.editingTransaction = entry;
    this.paymentForm = {
      date: entry.date.slice(0, 10),
      type: entry.type,
      category: entry.category,
      amount: entry.amount,
      notes: entry.notes || '',
      referenceNumber: entry.referenceNumber || ''
    };
    this.formMode = 'edit';
  }

  openFeeForm(): void {
    this.feeForm = this.emptyFeeForm();
    this.formMode = 'fee';
  }

  openInvoiceForm(): void {
    this.errorMessage = '';
    this.selectedFeeStructureId = 0;
    this.formMode = 'invoice';
  }

  closeForm(): void {
    if (!this.saving) {
      this.formMode = null;
    }
  }

  saveTransaction(): void {
    if (!this.selectedLearner || !this.isTransactionFormValid()) {
      this.errorMessage = 'Enter a valid date, category, and amount greater than zero.';
      return;
    }

    this.saving = true;
    this.errorMessage = '';
    this.successMessage = '';
    const request: LedgerTransactionUpsertRequest = {
      ...this.paymentForm,
      amount: Number(this.paymentForm.amount)
    };
    const saveRequest = this.formMode === 'edit' && this.editingTransaction
      ? this.financeApi.updateTransaction(this.schoolId, this.editingTransaction.transactionId, request)
      : this.financeApi.recordTransaction(this.schoolId, this.selectedLearner.learnerId, request);

    saveRequest.subscribe({
      next: () => {
        this.saving = false;
        this.formMode = null;
        this.successMessage = this.editingTransaction ? 'Ledger transaction updated.' : 'Payment recorded.';
        this.editingTransaction = null;
        this.refreshSelectedLearner();
      },
      error: error => {
        this.saving = false;
        this.errorMessage = this.getErrorMessage(error, 'Unable to save the ledger transaction.');
      }
    });
  }

  saveAdHocInvoice(): void {
    if (!this.selectedLearner
      || !this.paymentForm.date
      || !Number.isFinite(Number(this.paymentForm.amount))
      || Number(this.paymentForm.amount) <= 0
      || !this.paymentForm.notes.trim()) {
      this.errorMessage = 'Enter a description, valid date, and amount greater than zero.';
      return;
    }

    const learnerName = `${this.selectedLearner.firstName} ${this.selectedLearner.surname}`;
    const request: LedgerTransactionUpsertRequest = {
      ...this.paymentForm,
      type: 'DEBIT',
      category: 'Ad-hoc Invoice',
      notes: this.paymentForm.notes.trim(),
      amount: Number(this.paymentForm.amount)
    };

    this.saving = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.financeApi.recordTransaction(this.schoolId, this.selectedLearner.learnerId, request).subscribe({
      next: () => {
        this.saving = false;
        this.formMode = null;
        this.successMessage = `Ad-hoc invoice added for ${learnerName}.`;
        this.refreshSelectedLearner();
      },
      error: error => {
        this.saving = false;
        this.errorMessage = this.getErrorMessage(error, 'Unable to add the ad-hoc invoice.');
      }
    });
  }

  saveFeeStructure(): void {
    if (!this.isFeeFormValid()) {
      this.errorMessage = 'Complete the fee details and enter an amount greater than zero.';
      return;
    }

    const request: FeeStructureUpsertRequest = {
      ...this.feeForm,
      amount: Number(this.feeForm.amount),
      academicYear: Number(this.feeForm.academicYear)
    };
    this.saving = true;
    this.errorMessage = '';
    this.financeApi.createFeeStructure(this.schoolId, request).subscribe({
      next: () => {
        this.saving = false;
        this.formMode = null;
        this.successMessage = 'Fee structure created.';
        this.loadFeeStructures();
      },
      error: error => {
        this.saving = false;
        this.errorMessage = this.getErrorMessage(error, 'Unable to create the fee structure.');
      }
    });
  }

  generateInvoices(): void {
    if (this.selectedFeeStructureId <= 0) {
      this.errorMessage = 'Choose a fee structure before generating invoices.';
      return;
    }

    this.saving = true;
    this.errorMessage = '';
    this.financeApi.generateInvoices(this.schoolId, Number(this.selectedFeeStructureId)).subscribe({
      next: result => {
        this.saving = false;
        this.formMode = null;
        this.successMessage = result.message;
        this.loadDashboard();
      },
      error: error => {
        this.saving = false;
        this.errorMessage = this.getErrorMessage(error, 'Unable to generate invoices.');
      }
    });
  }

  voidTransaction(entry: StudentLedgerEntryDto): void {
    if (entry.isVoided || !window.confirm(`Void transaction #${entry.transactionId}? This will recalculate the learner balance.`)) {
      return;
    }

    this.errorMessage = '';
    this.successMessage = '';
    this.financeApi.voidTransaction(this.schoolId, entry.transactionId).subscribe({
      next: () => {
        this.successMessage = `Transaction #${entry.transactionId} voided.`;
        this.refreshSelectedLearner();
      },
      error: error => {
        this.errorMessage = this.getErrorMessage(error, 'Unable to void the transaction.');
      }
    });
  }

  printReceipt(entry: StudentLedgerEntryDto): void {
    if (!this.selectedLearner) {
      return;
    }

    const receiptWindow = window.open('about:blank', '_blank');
    if (!receiptWindow) {
      this.errorMessage = 'Allow pop-ups to print the receipt.';
      return;
    }

    this.errorMessage = '';
    this.financeApi.getTransactionPdf(this.schoolId, this.selectedLearner.learnerId, entry.transactionId).subscribe({
      next: pdf => {
        const pdfUrl = URL.createObjectURL(pdf);
        if (receiptWindow.closed) {
          URL.revokeObjectURL(pdfUrl);
          return;
        }

        receiptWindow.location.href = pdfUrl;
        receiptWindow.focus();
        window.setTimeout(() => URL.revokeObjectURL(pdfUrl), 5 * 60 * 1000);
      },
      error: error => {
        receiptWindow.close();
        this.errorMessage = this.getErrorMessage(error, 'Unable to open the invoice or payment receipt.');
      }
    });
  }

  formatAmount(amount: number): string {
    return Number(amount || 0).toLocaleString('en-ZA', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('en-ZA', { year: 'numeric', month: 'short', day: 'numeric' });
  }

  trackByLearnerId(_index: number, learner: FinanceLearnerBalanceDto): number {
    return learner.learnerId;
  }

  trackByTransactionId(_index: number, entry: StudentLedgerEntryDto): number {
    return entry.transactionId;
  }

  private refreshSelectedLearner(): void {
    this.loadDashboard();
    this.loadLedger();
  }

  private isTransactionFormValid(): boolean {
    return !!this.paymentForm.date
      && !!this.paymentForm.category.trim()
      && Number.isFinite(Number(this.paymentForm.amount))
      && Number(this.paymentForm.amount) > 0;
  }

  private isFeeFormValid(): boolean {
    return !!this.feeForm.grade.trim()
      && !!this.feeForm.term.trim()
      && !!this.feeForm.category.trim()
      && !!this.feeForm.description.trim()
      && Number.isInteger(Number(this.feeForm.academicYear))
      && Number(this.feeForm.amount) > 0;
  }

  private getErrorMessage(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse) {
      const responseError = error.error?.error;
      if (typeof responseError === 'string' && responseError.trim()) {
        return responseError;
      }
    }
    return fallback;
  }

  private getToday(): string {
    const now = new Date();
    const year = now.getFullYear();
    const month = `${now.getMonth() + 1}`.padStart(2, '0');
    const day = `${now.getDate()}`.padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  private emptyTransactionForm(): LedgerTransactionUpsertRequest {
    return {
      date: this.getToday(),
      type: 'CREDIT',
      category: 'Tuition Payment',
      amount: 0,
      notes: '',
      referenceNumber: ''
    };
  }

  private emptyFeeForm(): FeeStructureUpsertRequest {
    return {
      grade: '',
      term: 'Term 1',
      academicYear: new Date().getFullYear(),
      category: 'Tuition',
      description: '',
      amount: 0
    };
  }
}
