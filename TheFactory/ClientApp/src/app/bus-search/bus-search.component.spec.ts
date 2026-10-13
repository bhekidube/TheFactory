/// <reference types="jasmine" />

import { FormsModule } from '@angular/forms';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { Router } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';

import { BusSearchComponent } from './bus-search.component';
import { AuthService } from '../services/auth.service';

describe('BusSearchComponent', () => {
  let component: BusSearchComponent;
  let fixture: ComponentFixture<BusSearchComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BusSearchComponent ],
      imports: [FormsModule, HttpClientTestingModule, NoopAnimationsModule, RouterTestingModule],
      schemas: [NO_ERRORS_SCHEMA]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(BusSearchComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => {
    localStorage.removeItem('userName');
    localStorage.removeItem('userRole');
    localStorage.removeItem('userId');
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('shows the sign-in card when no authenticated session is present', () => {
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('app-auth')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('.account-card')).toBeNull();
  });

  it('replaces the sign-in card with account information for an authenticated user', () => {
    localStorage.setItem('userName', 'Bheki');
    localStorage.setItem('userRole', 'OperatorAdmin');
    localStorage.setItem('userId', '42');

    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('app-auth')).toBeNull();
    expect(fixture.nativeElement.querySelector('.account-card')?.textContent).toContain('Welcome back, Bheki');
    expect(fixture.nativeElement.querySelector('.account-card-role')?.textContent).toContain('OperatorAdmin');
  });

  it('shows the promotional image card in the right sidebar', () => {
    const promoCard = fixture.nativeElement.querySelector('.account-sidebar.promo-sidebar-card');
    const promoImage = promoCard?.querySelector('.promo-card-img') as HTMLImageElement;

    expect(promoCard).not.toBeNull();
    expect(promoCard.classList).toContain('account-card');
    expect(promoImage.getAttribute('src')).toBe('assets/hamba-insta-reel.jpeg');
    expect(promoImage.alt).toBe('HambaOnline Instagram Reel featuring a traveler at a bus terminal');
  });

  it('logs out through AuthService and navigates to the login page', () => {
    const authService = TestBed.inject(AuthService);
    const router = TestBed.inject(Router);
    const logoutSpy = spyOn(authService, 'logout');
    const navigateSpy = spyOn(router, 'navigate').and.resolveTo(true);

    component.logout();

    expect(logoutSpy).toHaveBeenCalled();
    expect(navigateSpy).toHaveBeenCalledWith(['/auth']);
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
