import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { of, forkJoin } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { LearnerManagementService } from '../services/learner-management.service';
import { LearnerDto } from '../learner-management/learner-management.models';

@Component({
  selector: 'app-learning-school',
  templateUrl: './learning-school.component.html',
  styleUrls: ['./learning-school.component.css']
})
export class LearningSchoolComponent implements OnInit {
  schoolId = '';
  schoolName = '';
  schoolsCount = 0;
  learnersCount = 0;
  loading = false;
  errorMessage = '';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly learnerService: LearnerManagementService
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.schoolId = params.get('schoolId') || '';
      this.loadSchoolContext();
    });
  }

  private loadSchoolContext(): void {
    this.loading = true;
    this.errorMessage = '';
    this.schoolName = '';

    this.learnerService.getLearners().subscribe({
      next: (learners: LearnerDto[]) => {
        this.learnersCount = learners.length;

        if (!learners.length) {
          this.loading = false;
          this.errorMessage = 'No school records are available.';
          return;
        }

        const reportRequests = learners.map(learner =>
          this.learnerService.getReport(learner.id).pipe(catchError(() => of(null)))
        );

        forkJoin(reportRequests).subscribe({
          next: reports => {
            const schoolNames = Array.from(new Set(
              reports
                .map(report => report?.schoolName?.trim())
                .filter((name): name is string => !!name)
            )).sort((a, b) => a.localeCompare(b));

            this.schoolsCount = schoolNames.length;

            const selected = schoolNames.find(name => this.toSchoolId(name) === this.schoolId);
            if (!selected) {
              this.errorMessage = 'Selected school was not found.';
            } else {
              this.schoolName = selected;
            }

            this.loading = false;
          },
          error: () => {
            this.loading = false;
            this.errorMessage = 'Unable to load school context right now.';
          }
        });
      },
      error: () => {
        this.loading = false;
        this.errorMessage = 'Unable to load school context right now.';
      }
    });
  }

  private toSchoolId(name: string): string {
    return name
      .trim()
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-+|-+$/g, '');
  }
}
