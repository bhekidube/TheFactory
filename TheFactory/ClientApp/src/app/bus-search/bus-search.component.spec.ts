/// <reference types="jasmine" />

import { FormsModule } from '@angular/forms';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { NO_ERRORS_SCHEMA } from '@angular/core';

import { BusSearchComponent } from './bus-search.component';

describe('BusSearchComponent', () => {
  let component: BusSearchComponent;
  let fixture: ComponentFixture<BusSearchComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BusSearchComponent ],
      imports: [FormsModule, HttpClientTestingModule, NoopAnimationsModule],
      schemas: [NO_ERRORS_SCHEMA]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(BusSearchComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render a seat-selection CTA for each future search result', () => {
    const futureDeparture = new Date(Date.now() + 60 * 60 * 1000).toISOString();

    component.searchResults = [{
      operatorName: 'Bravo Coach',
      departureDateTime: futureDeparture,
      price: 120
    }];

    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('.trip-action-btn') as HTMLButtonElement;
    expect(button).not.toBeNull();
    expect(button.textContent).toContain('Select Seat');
  });
});
