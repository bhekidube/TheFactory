import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { FormsModule } from '@angular/forms';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { of } from 'rxjs';
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

  it('initializes Google sign-in after the button container is mounted', () => {
    const buttonContainer = fixture.nativeElement.querySelector('#google-signin-button');

    expect(buttonContainer).not.toBeNull();
    expect(initializeGoogleSignInSpy).toHaveBeenCalledWith(buttonContainer, jasmine.any(Function));
  });

  it('omits address fields from the registration form and model', () => {
    component.showRegister = true;
    fixture.detectChanges();

    const registrationForm = fixture.nativeElement.querySelectorAll('form')[1];

    expect(registrationForm.textContent).not.toContain('Address');
    expect(registrationForm.querySelector('[name="registerAddressLine1"]')).toBeNull();
    expect(registrationForm.querySelector('[name="registerAddressLine2"]')).toBeNull();
    expect(registrationForm.querySelector('[name="registerCity"]')).toBeNull();
    expect(registrationForm.querySelector('[name="registerProvinceState"]')).toBeNull();
    expect(registrationForm.querySelector('[name="registerPostalCode"]')).toBeNull();
    expect(registrationForm.querySelector('[name="registerCountry"]')).toBeNull();
    expect('address' in component.registerModel).toBeFalse();
  });

  it('requires and saves an international cellphone before completing Google sign-in', () => {
    loginWithGoogleSpy.and.returnValue(of({
      userName: 'Bheki',
      userRole: 'OperatorAdmin',
      userId: 42,
      cellPhoneNo: null
    }));
    const googleCallback = initializeGoogleSignInSpy.calls.mostRecent().args[1] as (credential: string) => void;

    googleCallback('google-credential');
    fixture.detectChanges();

    expect(component.showGoogleProfileCompletion).toBeTrue();
    expect(localStorage.getItem('userId')).toBeNull();

    const phoneInput = fixture.nativeElement.querySelector('#googleProfilePhone') as HTMLInputElement;
    phoneInput.value = '+263771234567';
    phoneInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const form = fixture.nativeElement.querySelector('.google-profile-completion form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(completeGoogleProfileSpy).toHaveBeenCalledWith('google-credential', '+263771234567');

    expect(component.showGoogleProfileCompletion).toBeFalse();
    expect(localStorage.getItem('cellPhoneNo')).toBe('+263771234567');
    expect(localStorage.getItem('userId')).toBe('42');
  });

  it('defaults both registration phone fields to +27 and validates international formats', () => {
    component.showRegister = true;
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

    expect(fixture.nativeElement.textContent).toContain('Enter a valid international number');
    expect(fixture.nativeElement.textContent).toContain('Alternate cell phone number is required.');
  });
});
