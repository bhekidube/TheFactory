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
});
