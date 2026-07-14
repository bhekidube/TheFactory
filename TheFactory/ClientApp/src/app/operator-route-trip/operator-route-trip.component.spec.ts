import { ComponentFixture, TestBed } from '@angular/core/testing';

import { OperatorRouteTripComponent } from './operator-route-trip.component';

describe('OperatorRouteTripComponent', () => {
  let component: OperatorRouteTripComponent;
  let fixture: ComponentFixture<OperatorRouteTripComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ OperatorRouteTripComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(OperatorRouteTripComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
