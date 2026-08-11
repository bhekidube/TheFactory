import { Component, OnInit } from '@angular/core';
import { of, forkJoin } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { LearnerManagementService } from '../services/learner-management.service';
import { LearnerDto } from '../learner-management/learner-management.models';

@Component({
  selector: 'app-learning',
  templateUrl: './learning.component.html',
  styleUrls: ['./learning.component.css']
})
export class LearningComponent implements OnInit {
  schools: string[] = [];
  learnersCount = 0;
  loading = false;
  errorMessage = '';

  constructor(private readonly learnerService: LearnerManagementService) {}

  ngOnInit(): void {
    this.loadSchools();
  }

  private loadSchools(): void {
    this.loading = true;
    this.errorMessage = '';

    this.learnerService.getLearners().subscribe({
      next: (learners: LearnerDto[]) => {
        this.learnersCount = learners.length;

        if (!learners.length) {
          this.loading = false;
          return;
        }

        const reportRequests = learners.map(learner =>
          this.learnerService.getReport(learner.id).pipe(catchError(() => of(null)))
        );

        forkJoin(reportRequests).subscribe({
          next: reports => {
            const schoolSet = new Set<string>();

            reports.forEach(report => {
              const schoolName = report?.schoolName?.trim();
              if (schoolName) {
                schoolSet.add(schoolName);
              }
            });

            this.schools = Array.from(schoolSet).sort((a, b) => a.localeCompare(b));
            this.loading = false;
          },
          error: () => {
            this.loading = false;
            this.errorMessage = 'Unable to load schools right now.';
          }
        });
      },
      error: () => {
        this.loading = false;
        this.errorMessage = 'Unable to load schools right now.';
      }
    });
  }
}
