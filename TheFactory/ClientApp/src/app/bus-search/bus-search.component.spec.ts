/// <reference types="jasmine" />

import { FormsModule } from '@angular/forms';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
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

  it('shows the cross-border marquee in two columns in the right sidebar', () => {
    const marquee = fixture.nativeElement.querySelector('.account-sidebar.transit-marquee-card');

    expect(marquee).not.toBeNull();
    expect(marquee.classList).toContain('account-card');
    expect(marquee.querySelectorAll('.transit-marquee-column').length).toBe(2);
    expect(marquee.querySelector('.transit-marquee-track-up')).not.toBeNull();
    expect(marquee.querySelector('.transit-marquee-track-down')).not.toBeNull();
    expect(marquee.querySelector('.visually-hidden')?.textContent).toContain('Brooklyn Express');
    expect(marquee.querySelector('.visually-hidden')?.textContent).toContain('Swiss');
  });

  it('logs out through AuthService and navigates to the home page', () => {
    const authService = TestBed.inject(AuthService);
    const router = TestBed.inject(Router);
    const logoutSpy = spyOn(authService, 'logout');
    const navigateSpy = spyOn(router, 'navigate').and.resolveTo(true);

    component.logout();

    expect(logoutSpy).toHaveBeenCalled();
    expect(navigateSpy).toHaveBeenCalledWith(['/']);
  });

  it('should render a seat-selection CTA for each future search result', () => {
    localStorage.setItem('userName', 'Bheki');
    localStorage.setItem('userRole', 'OperatorAdmin');
    localStorage.setItem('userId', '42');
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

  it('links bus names to operator admin pages for SystemAdmin users', () => {
    localStorage.setItem('userName', 'Bheki');
    localStorage.setItem('userRole', 'SystemAdmin');
    localStorage.setItem('userId', '42');
    component.searchResults = [{
      operatorName: 'Brooklyn Express',
      departureDateTime: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
      price: 120
    }];

    fixture.detectChanges();

    const link = fixture.nativeElement.querySelector('tbody td.col-bus .bus-admin-link') as HTMLAnchorElement;
    expect(link).not.toBeNull();
    expect(link.textContent).toContain('Brooklyn Express');
    expect(link.getAttribute('href')).toBe('/operator-admin/Brooklyn%20Express');
    expect(fixture.nativeElement.querySelector('.trip-action-btn')).not.toBeNull();
  });

  it('renders bus names as plain text for users without the SystemAdmin role', () => {
    localStorage.setItem('userName', 'Bheki');
    localStorage.setItem('userRole', 'OperatorAdmin');
    localStorage.setItem('userId', '42');
    component.searchResults = [{
      operatorName: 'Revival',
      departureDateTime: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
      price: 120
    }];

    fixture.detectChanges();

    const busCell = fixture.nativeElement.querySelector('tbody td.col-bus') as HTMLElement;
    expect(busCell.textContent).toContain('Revival');
    expect(busCell.querySelector('a')).toBeNull();
  });

  it('allows SystemAdmin users to edit and save a departure time inline', () => {
    localStorage.setItem('userName', 'Bheki');
    localStorage.setItem('userRole', 'SystemAdmin');
    localStorage.setItem('userId', '42');
    component.searchResults = [{
      tripId: 7,
      operatorName: 'Revival',
      departureDateTime: '2026-11-12T10:30:00',
      arrivalDateTime: '2026-11-12T18:00:00',
      price: 120,
      notes: '',
      active: true
    }];
    fixture.detectChanges();

    const editButton = fixture.nativeElement.querySelector('.departure-edit-trigger') as HTMLButtonElement;
    expect(editButton).not.toBeNull();
    editButton.click();
    fixture.detectChanges();

    const input = fixture.nativeElement.querySelector('.departure-edit-input') as HTMLInputElement;
    expect(input).not.toBeNull();
    expect(input.value).toBe('2026-11-12T10:30');
    expect(fixture.nativeElement.querySelector('.trip-action-btn')).not.toBeNull();

    component.departureDateTimeDraft = '2026-11-12T11:15';
    component.saveDepartureDateTime(component.searchResults[0]);

    const http = TestBed.inject(HttpTestingController);
    const request = http.expectOne(req => req.url.endsWith('/api/BusTrips/UpdateRouteTrip'));
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual(jasmine.objectContaining({
      tripId: 7,
      departureDateTime: '2026-11-12T11:15',
      updatedBy: 42
    }));
    request.flush({ rowsAffected: 1 });
    expect(component.searchResults[0].departureDateTime).toBe(new Date('2026-11-12T11:15').toISOString());
    expect(component.editingDepartureTripId).toBeNull();
  });

  it('does not expose inline departure editing to non-SystemAdmin users', () => {
    localStorage.setItem('userName', 'Bheki');
    localStorage.setItem('userRole', 'Admin');
    localStorage.setItem('userId', '42');
    component.searchResults = [{
      tripId: 7,
      operatorName: 'Revival',
      departureDateTime: '2026-11-12T10:30:00'
    }];

    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.departure-edit-trigger')).toBeNull();
    expect(fixture.nativeElement.querySelector('.departure-edit-input')).toBeNull();
  });

  it('allows SystemAdmin users to save a price inline with Enter', () => {
    localStorage.setItem('userName', 'Bheki');
    localStorage.setItem('userRole', 'SystemAdmin');
    localStorage.setItem('userId', '42');
    component.searchResults = [{
      tripId: 7,
      operatorName: 'Revival',
      departureDateTime: '2026-11-12T10:30:00',
      arrivalDateTime: '2026-11-12T18:00:00',
      price: 700,
      notes: '',
      active: true
    }];
    fixture.detectChanges();

    const editButton = fixture.nativeElement.querySelector('.price-edit-trigger') as HTMLButtonElement;
    expect(editButton).not.toBeNull();
    editButton.click();
    fixture.detectChanges();

    const input = fixture.nativeElement.querySelector('.price-edit-input') as HTMLInputElement;
    expect(input).not.toBeNull();
    expect(input.value).toBe('700');
    expect(fixture.nativeElement.querySelector('.trip-action-btn')).not.toBeNull();

    input.value = '725.5';
    input.dispatchEvent(new Event('input'));
    input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));

    const http = TestBed.inject(HttpTestingController);
    const request = http.expectOne(req => req.url.endsWith('/api/BusTrips/UpdateRouteTrip'));
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual(jasmine.objectContaining({
      tripId: 7,
      departureDateTime: '2026-11-12T10:30:00',
      price: 725.5,
      updatedBy: 42
    }));
    request.flush({ rowsAffected: 1 });
    expect(component.searchResults[0].price).toBe(725.5);
    expect(component.editingPriceTripId).toBeNull();
  });

  it('renders prices as plain text for users without the SystemAdmin role', () => {
    localStorage.setItem('userName', 'Bheki');
    localStorage.setItem('userRole', 'Admin');
    localStorage.setItem('userId', '42');
    component.searchResults = [{
      tripId: 7,
      operatorName: 'Revival',
      departureDateTime: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
      price: 700
    }];

    fixture.detectChanges();

    const priceCell = fixture.nativeElement.querySelector('tbody td.col-price') as HTMLElement;
    expect(priceCell.textContent).toContain('R 700.00');
    expect(priceCell.querySelector('.price-edit-trigger')).toBeNull();
    expect(priceCell.querySelector('.price-edit-input')).toBeNull();
  });

  it('offers guests a sign-in action instead of Select Seat', () => {
    component.searchResults = [{
      operatorName: 'Bravo Coach',
      departureDateTime: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
      price: 120
    }];

    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('.trip-action-btn') as HTMLButtonElement;
    expect(button.textContent).toContain('Sign in to select seat');
    expect(button.textContent).not.toContain('Select Seat');
    expect(button.disabled).toBeFalse();
  });

  it('does not treat an incomplete stored session as authenticated', () => {
    localStorage.setItem('userName', 'Bheki');
    localStorage.setItem('userRole', 'OperatorAdmin');

    expect(component.isLoggedIn).toBeFalse();
  });

  it('redirects guests to the login page when they activate the seat action', () => {
    const router = TestBed.inject(Router);
    const navigateSpy = spyOn(router, 'navigate').and.resolveTo(true);
    const trip = {
      operatorName: 'Bravo Coach',
      departureDateTime: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
      price: 120
    };

    component.onSelectSeat(trip);

    expect(navigateSpy).toHaveBeenCalledWith(['/auth']);
  });

  it('does not render the Facebook posting button beneath bus schedules', () => {
    component.searchResults = [{
      operatorName: 'Bravo Coach',
      departureDateTime: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
      price: 120
    }];

    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.facebook-share-btn')).toBeNull();
    expect(fixture.nativeElement.textContent).not.toContain('Post to Facebook');
  });
});
