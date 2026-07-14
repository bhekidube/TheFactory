import { ComponentFixture, TestBed } from '@angular/core/testing';

import { OperatorAdminComponent } from './operator-admin.component';

describe('OperatorAdminComponent', () => {
  let component: OperatorAdminComponent;
  let fixture: ComponentFixture<OperatorAdminComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ OperatorAdminComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(OperatorAdminComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
