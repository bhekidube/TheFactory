import { Component, OnInit } from '@angular/core';
import { LearnerManagementService } from '../services/learner-management.service';

@Component({
  selector: 'app-learning',
  templateUrl: './learning.component.html',
  styleUrls: ['./learning.component.css']
})
export class LearningComponent implements OnInit {
  schools: Array<{ id: string; name: string }> = [];
  loading = false;
  errorMessage = '';

  constructor(private readonly learnerService: LearnerManagementService) {}

  ngOnInit(): void {
    this.learnerService.clearSelectedTenantId();
    this.loadSchools();
  }

  private loadSchools(): void {
    this.loading = true;
    this.errorMessage = '';

    this.learnerService.getSchools().subscribe({
      next: schools => {
        this.schools = schools.map(school => ({
          id: school.id.toString(),
          name: school.name
        }));
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.errorMessage = 'Unable to load schools right now.';
      }
    });
  }
}
