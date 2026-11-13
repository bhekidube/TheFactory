import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { FormsModule } from '@angular/forms';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { of, throwError } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthService } from '../services/auth.service';

import { AuthComponent } from './auth.component';

describe('AuthComponent', () => {
  let component: AuthComponent;
  let fixture: ComponentFixture<AuthComponent>;
  let initializeGoogleSignInSpy: jasmine.Spy;
  let loginWithGoogleSpy: jasmine.Spy;
  let completeGoogleProfileSpy: jasmine.Spy;
  let httpTestingController: HttpTestingController;

  beforeEach(async () => {
    initializeGoogleSignInSpy = jasmine.createSpy('initializeGoogleSignIn').and.returnValue(Promise.resolve());
    loginWithGoogleSpy = jasmine.createSpy('loginWithGoogle');
    completeGoogleProfileSpy = jasmine.createSpy('completeGoogleProfile').and.returnValue(of({
      cellPhoneNo: '+263771234567'
    }));

    await TestBed.configureTestingModule({
      declarations: [ AuthComponent ],
      imports: [FormsModule, HttpClientTestingModule, RouterTestingModule],
      providers: [{
        provide: AuthService,
        useValue: {
          initializeGoogleSignIn: initializeGoogleSignInSpy,
          loginWithGoogle: loginWithGoogleSpy,
          completeGoogleProfile: completeGoogleProfileSpy,
          logout: () => undefined
        }
      }]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(AuthComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    httpTestingController = TestBed.inject(HttpTestingController);
    httpTestingController.expectOne(`${environment.apiBaseUrl}/api/LookUp/GetAllOperators`).flush([]);
  });

  afterEach(() => {
    httpTestingController.verify();
    localStorage.clear();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('redirects credential-authenticated SystemAdmin users to the home page', () => {
    const router = TestBed.inject(Router);
    const navigateSpy = spyOn(router, 'navigate').and.resolveTo(true);
    component.loginModel = { email: 'admin@example.com', password: 'password123' };

    component.login();

    const loginRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}/api/User/Login`);
    loginRequest.flush({ userName: 'Admin User', userRole: 'SystemAdmin', userId: 7 });

    expect(navigateSpy).toHaveBeenCalledWith(['/']);
    expect(localStorage.getItem('userRole')).toBe('SystemAdmin');
  });

  it('initializes Google sign-in after the button container is mounted', () => {
    const buttonContainer = fixture.nativeElement.querySelector('#google-signin-button');

    expect(buttonContainer).not.toBeNull();
    expect(initializeGoogleSignInSpy).toHaveBeenCalledWith(buttonContainer, jasmine.any(Function), 'signin_with');
  });

  it('places Google sign-up above the registration form and provisions a Customer account', () => {
    const router = TestBed.inject(Router);
    const navigateSpy = spyOn(router, 'navigate').and.resolveTo(true);
    component.toggleRegister();
    component.operators = [{ operatorId: 9, name: 'Regional' }];
    loginWithGoogleSpy.and.returnValues(
      throwError(() => ({
        status: 409,
        error: { code: 'GoogleRegistrationRequired', error: 'Create a Customer account.' }
      })),
      of({
        userName: 'New Customer',
        userRole: 'Customer',
        userId: 27,
        operatorId: 9,
        cellPhoneNo: '+263771234567'
      })
    );
    fixture.detectChanges();

    const googleButton = fixture.nativeElement.querySelector('.social-login') as HTMLElement;
    const divider = fixture.nativeElement.querySelector('.auth-divider') as HTMLElement;
    const registrationForm = fixture.nativeElement.querySelector('#register-form') as HTMLFormElement;
    expect(googleButton).not.toBeNull();
    expect(divider.textContent).toContain('OR');
    expect(googleButton.compareDocumentPosition(registrationForm) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    expect(fixture.nativeElement.querySelector('#loginEmail')).toBeNull();

    const googleCallback = initializeGoogleSignInSpy.calls.mostRecent().args[1] as (credential: string) => void;
    googleCallback('new-google-credential');
    fixture.detectChanges();

    expect(component.showGoogleRegistrationCompletion).toBeTrue();
    component.googleRegistrationOperatorId = 9;
    component.googleRegistrationCellPhoneNo = '+263771234567';
    component.googleRegistrationAlternateCellPhoneNo = '+263772345678';
    component.completeGoogleRegistration();

    expect(loginWithGoogleSpy).toHaveBeenCalledTimes(2);
    expect(loginWithGoogleSpy.calls.mostRecent().args).toEqual([
      'new-google-credential',
      {
        operatorId: 9,
        cellPhoneNo: '+263771234567',
        alternateCellPhoneNo: '+263772345678'
      }
    ]);
    expect(localStorage.getItem('userRole')).toBe('Customer');
    expect(navigateSpy).toHaveBeenCalledWith(['/']);
  });

  it('omits address fields from the registration form and model', () => {
    component.showRegister = true;
    fixture.detectChanges();

    const registrationForm = fixture.nativeElement.querySelector('#register-form') as HTMLFormElement;

    expect(registrationForm.textContent).not.toContain('Address');
    expect(registrationForm.querySelector('[name="registerAddressLine1"]')).toBeNull();
    expect(registrationForm.querySelector('[name="registerAddressLine2"]')).toBeNull();
    expect(registrationForm.querySelector('[name="registerCity"]')).toBeNull();
    expect(registrationForm.querySelector('[name="registerProvinceState"]')).toBeNull();
    expect(registrationForm.querySelector('[name="registerPostalCode"]')).toBeNull();
    expect(registrationForm.querySelector('[name="registerCountry"]')).toBeNull();
    expect('address' in component.registerModel).toBeFalse();
  });

  it('requires and saves an international cellphone before completing Google sign-in', async () => {
    loginWithGoogleSpy.and.returnValue(of({
      userName: 'Bheki',
      userRole: 'OperatorAdmin',
      userId: 42,
      cellPhoneNo: null
    }));
    const googleCallback = initializeGoogleSignInSpy.calls.mostRecent().args[1] as (credential: string) => void;

    googleCallback('google-credential');
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(component.showGoogleProfileCompletion).toBeTrue();
    expect(localStorage.getItem('userId')).toBeNull();

    const phoneInput = fixture.nativeElement.querySelector('#googleProfilePhone') as HTMLInputElement;
    phoneInput.value = '+263771234567';
    phoneInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const form = fixture.nativeElement.querySelector('.google-profile-completion form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(completeGoogleProfileSpy).toHaveBeenCalledWith('google-credential', '+263771234567');

    expect(component.showGoogleProfileCompletion).toBeFalse();
    expect(localStorage.getItem('cellPhoneNo')).toBe('+263771234567');
    expect(localStorage.getItem('userId')).toBe('42');
  });

  it('defaults both registration phone fields to +27 and validates international formats', async () => {
    component.showRegister = true;
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const primaryPhone = fixture.nativeElement.querySelector('[name="registerCellPhoneNo"]') as HTMLInputElement;
    const alternatePhone = fixture.nativeElement.querySelector('[name="registerAlternateCellPhoneNo"]') as HTMLInputElement;

    expect(primaryPhone.type).toBe('tel');
    expect(primaryPhone.placeholder).toBe('+27...');
    expect(primaryPhone.value).toBe('+27');
    expect(alternatePhone.type).toBe('tel');
    expect(alternatePhone.placeholder).toBe('+27...');
    expect(alternatePhone.value).toBe('+27');

    primaryPhone.value = '27821234567';
    primaryPhone.dispatchEvent(new Event('input'));
    primaryPhone.dispatchEvent(new Event('blur'));
    alternatePhone.value = '';
    alternatePhone.dispatchEvent(new Event('input'));
    alternatePhone.dispatchEvent(new Event('blur'));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Enter a valid international number');
    expect(fixture.nativeElement.textContent).toContain('Alternate cell phone number is required.');
  });
});
