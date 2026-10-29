import { HttpClientTestingModule } from '@angular/common/http/testing';
import { FormsModule } from '@angular/forms';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { AuthService } from '../services/auth.service';

import { AuthComponent } from './auth.component';

describe('AuthComponent', () => {
  let component: AuthComponent;
  let fixture: ComponentFixture<AuthComponent>;
  let initializeGoogleSignInSpy: jasmine.Spy;

  beforeEach(async () => {
    initializeGoogleSignInSpy = jasmine.createSpy('initializeGoogleSignIn').and.returnValue(Promise.resolve());

    await TestBed.configureTestingModule({
      declarations: [ AuthComponent ],
      imports: [FormsModule, HttpClientTestingModule, RouterTestingModule],
      providers: [{
        provide: AuthService,
        useValue: {
          initializeGoogleSignIn: initializeGoogleSignInSpy,
          loginWithGoogle: () => { throw new Error('Unexpected Google login in component test.'); }
        }
      }]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(AuthComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
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
