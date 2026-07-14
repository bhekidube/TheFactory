import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ZimraDeclarationComponent } from './zimra-declaration.component';

describe('ZimraDeclarationComponent', () => {
  let component: ZimraDeclarationComponent;
  let fixture: ComponentFixture<ZimraDeclarationComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ZimraDeclarationComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(ZimraDeclarationComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
