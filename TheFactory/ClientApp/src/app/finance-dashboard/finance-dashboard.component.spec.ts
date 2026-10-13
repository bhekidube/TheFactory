/// <reference types="jasmine" />

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { FinanceDashboardComponent } from './finance-dashboard.component';
import { FinanceApiService } from '../services/finance-api.service';

describe('FinanceDashboardComponent', () => {
  let fixture: ComponentFixture<FinanceDashboardComponent>;
  let financeApi: jasmine.SpyObj<FinanceApiService>;

  beforeEach(async () => {
    financeApi = jasmine.createSpyObj<FinanceApiService>('FinanceApiService', [
      'getDashboard',
      'getFeeStructures',
      'getLedger'
    ]);
    financeApi.getDashboard.and.returnValue(of({
      totalBilled: 300,
      totalCollected: 100,
      outstandingBalance: 200,
      learners: [{
        learnerId: 3,
        firstName: 'Princess',
        surname: 'Chikuni',
        grade: 'ECD A',
        billed: 300,
        collected: 100,
        balance: 200
      }]
    }));
    financeApi.getFeeStructures.and.returnValue(of([]));
    financeApi.getLedger.and.returnValue(of([{
      transactionId: 9,
      learnerId: 3,
      date: '2026-01-05',
      type: 'DEBIT',
      category: 'Tuition',
      amount: 300,
      runningBalance: 300,
      notes: 'Term 1 fees',
      referenceNumber: 'INV-2026-TERM1-3',
      recordedBy: 1,
      isVoided: false
    }]));

    await TestBed.configureTestingModule({
      declarations: [FinanceDashboardComponent],
      imports: [FormsModule],
      providers: [
        { provide: FinanceApiService, useValue: financeApi },
        {
          provide: ActivatedRoute,
          useValue: { parent: { snapshot: { paramMap: { get: () => '12' } } } }
        }
      ]
    }).compileComponents();
  });

  it('loads school finance metrics and the selected learner ledger', () => {
    fixture = TestBed.createComponent(FinanceDashboardComponent);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent || '';
    expect(financeApi.getDashboard).toHaveBeenCalledWith(12);
    expect(financeApi.getLedger).toHaveBeenCalledWith(12, 3);
    expect(text).toContain('Total Billed');
    expect(text).toMatch(/300[.,]00/);
    expect(text).toContain('Princess Chikuni');
    expect(text).toContain('INV-2026-TERM1-3');
  });
});
