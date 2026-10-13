import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  BulkInvoiceResultDto,
  FeeStructureDto,
  FeeStructureUpsertRequest,
  FinanceDashboardDto,
  LedgerTransactionUpsertRequest,
  StudentLedgerEntryDto
} from '../finance-dashboard/finance.models';

@Injectable({ providedIn: 'root' })
export class FinanceApiService {
  private readonly baseUrl = `${environment.apiBaseUrl}/api`;

  constructor(private readonly http: HttpClient) {}

  getDashboard(schoolId: number): Observable<FinanceDashboardDto> {
    return this.http.get<FinanceDashboardDto>(`${this.baseUrl}/schools/${schoolId}/finance`, this.options(schoolId));
  }

  getLedger(schoolId: number, learnerId: number): Observable<StudentLedgerEntryDto[]> {
    return this.http.get<StudentLedgerEntryDto[]>(
      `${this.baseUrl}/schools/${schoolId}/finance/learners/${learnerId}/ledger`,
      this.options(schoolId)
    );
  }

  getFeeStructures(schoolId: number): Observable<FeeStructureDto[]> {
    return this.http.get<FeeStructureDto[]>(
      `${this.baseUrl}/schools/${schoolId}/finance/fee-structures`,
      this.options(schoolId)
    );
  }

  createFeeStructure(schoolId: number, request: FeeStructureUpsertRequest): Observable<FeeStructureDto> {
    return this.http.post<FeeStructureDto>(
      `${this.baseUrl}/schools/${schoolId}/finance/fee-structures`,
      request,
      this.options(schoolId)
    );
  }

  recordTransaction(
    schoolId: number,
    learnerId: number,
    request: LedgerTransactionUpsertRequest
  ): Observable<StudentLedgerEntryDto> {
    return this.http.post<StudentLedgerEntryDto>(
      `${this.baseUrl}/schools/${schoolId}/finance/learners/${learnerId}/transactions`,
      request,
      this.options(schoolId)
    );
  }

  updateTransaction(
    schoolId: number,
    transactionId: number,
    request: LedgerTransactionUpsertRequest
  ): Observable<StudentLedgerEntryDto> {
    return this.http.put<StudentLedgerEntryDto>(
      `${this.baseUrl}/schools/${schoolId}/finance/transactions/${transactionId}`,
      request,
      this.options(schoolId)
    );
  }

  voidTransaction(schoolId: number, transactionId: number): Observable<void> {
    return this.http.post<void>(
      `${this.baseUrl}/schools/${schoolId}/finance/transactions/${transactionId}/void`,
      {},
      this.options(schoolId)
    );
  }

  generateInvoices(schoolId: number, feeStructureId: number): Observable<BulkInvoiceResultDto> {
    return this.http.post<BulkInvoiceResultDto>(
      `${this.baseUrl}/schools/${schoolId}/finance/invoices`,
      { feeStructureId },
      this.options(schoolId)
    );
  }

  private options(schoolId: number): { headers: HttpHeaders } {
    const headers = new HttpHeaders({
      'X-Tenant-Id': schoolId.toString(),
      'X-User-Id': typeof localStorage === 'undefined' ? '' : localStorage.getItem('userId') || '',
      'X-User-Role': typeof localStorage === 'undefined' ? '' : localStorage.getItem('userRole') || ''
    });

    return { headers };
  }
}
