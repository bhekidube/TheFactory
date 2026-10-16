import { TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';

import { LearnerManagementComponent } from './learner-management.component';
import { LearnerDto } from './learner-management.models';
import { LearnerManagementService } from '../services/learner-management.service';

describe('LearnerManagementComponent sorting', () => {
  let component: LearnerManagementComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RouterTestingModule],
      providers: [{
        provide: LearnerManagementService,
        useValue: {}
      }]
    }).compileComponents();

    component = TestBed.createComponent(LearnerManagementComponent).componentInstance;
  });

  it('sorts learner IDs numerically and toggles the sort direction', () => {
    component.learners = [
      createLearner(20, 'Sam', 'Zulu', '2', '2015-01-01'),
      createLearner(3, 'Ben', 'Alpha', '1', '2016-01-01'),
      createLearner(11, 'Kim', 'Bravo', '3', '2014-01-01')
    ];
    component.applySearch();

    component.sort('id');
    expect(component.filteredLearners.map(learner => learner.id)).toEqual([3, 11, 20]);

    component.sort('id');
    expect(component.filteredLearners.map(learner => learner.id)).toEqual([20, 11, 3]);
  });

  it('keeps empty values at the bottom and preserves sorting when filtering', () => {
    component.learners = [
      createLearner(1, 'Zoe', 'Zulu', '—', '2015-01-01'),
      createLearner(2, 'Alice', 'Alpha', '2', '2016-01-01'),
      createLearner(3, 'Alex', 'Bravo', '1', '2014-01-01')
    ];
    component.applySearch();
    component.sort('grade');

    expect(component.filteredLearners.map(learner => learner.id)).toEqual([3, 2, 1]);

    component.searchTerm = 'al';
    component.applySearch();

    expect(component.filteredLearners.map(learner => learner.id)).toEqual([3, 2]);
  });
});

function createLearner(
  id: number,
  firstName: string,
  surname: string,
  grade: string,
  dateOfBirth: string
): LearnerDto {
  const address = {
    addressLine1: '',
    addressLine2: '',
    city: '',
    provinceState: '',
    postalCode: '',
    country: ''
  };

  return {
    id,
    firstName,
    surname,
    grade,
    dateOfBirth,
    learnerType: 'FullDay',
    address,
    parentGuardian: {
      firstName: '',
      surname: '',
      phoneNumber: '',
      emailAddress: '',
      relationshipToLearner: '',
      address
    }
  };
}
