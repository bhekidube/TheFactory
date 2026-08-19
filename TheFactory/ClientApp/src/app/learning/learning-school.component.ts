import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { LearnerManagementService } from '../services/learner-management.service';

@Component({
  selector: 'app-learning-school',
  templateUrl: './learning-school.component.html',
  styleUrls: ['./learning-school.component.css']
})
export class LearningSchoolComponent implements OnInit {
  schoolId = '';
  activeSection = 'learners';
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
    this.route.url.subscribe(segments => {
      this.activeSection = segments.length > 0 ? segments[segments.length - 1].path : 'learners';
    });

    this.route.paramMap.subscribe(params => {
      this.schoolId = params.get('schoolId') || '';
      const tenantId = Number(this.schoolId);
      if (Number.isFinite(tenantId) && tenantId > 0) {
        this.learnerService.setSelectedTenantId(tenantId);
      }
      this.loadSchoolContext();
    });
  }

  get isCurriculumView(): boolean {
    return this.activeSection === 'curriculum';
  }

  get isWorkView(): boolean {
    return this.activeSection === 'work';
  }

  private loadSchoolContext(): void {
    this.loading = true;
    this.errorMessage = '';
    this.schoolName = '';

    this.learnerService.getSchools().subscribe({
      next: schools => {
        this.schoolsCount = schools.length;

        const selectedSchool = schools.find(school => school.id.toString() === this.schoolId);
        if (!selectedSchool) {
          this.loading = false;
          this.errorMessage = 'Selected school was not found.';
          return;
        }

        this.schoolName = selectedSchool.name;

        this.learnerService.getLearners().subscribe({
          next: learners => {
            this.learnersCount = learners.length;
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
}
