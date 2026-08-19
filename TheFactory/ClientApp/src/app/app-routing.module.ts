import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthComponent } from './auth/auth.component'; 
import { AdminScreenComponent } from './admin-screen/admin-screen.component';
import { OperatorAdminComponent } from './operator-admin/operator-admin.component';
import { ZimraComponent } from './zimra/zimra.component';
import { FaresComponent } from './fares/fares.component';
import { LearnerManagementComponent } from './learner-management/learner-management.component';
import { LearningComponent } from './learning/learning.component';
import { LearningSchoolComponent } from './learning/learning-school.component';
import { BusSearchComponent } from './bus-search/bus-search.component';
import { ClassDetailComponent } from './class-detail/class-detail.component';
import { CurriculumViewComponent } from './curriculum-view/curriculum-view.component';
import { LearnerDetailComponent } from './learner-detail/learner-detail.component';
import { WorkManagementComponent } from './work-management/work-management.component';

const routes: Routes = [
  { path: '', component: BusSearchComponent, pathMatch: 'full' },
  { path: 'auth', component: AuthComponent },
  { path: 'learning', component: LearningComponent },
  { path: 'learning/schools/:schoolId', redirectTo: 'learning/schools/:schoolId/learners', pathMatch: 'full' },
  { path: 'learning/schools/:schoolId/learners', component: LearningSchoolComponent },
  { path: 'learning/schools/:schoolId/reports', component: LearningSchoolComponent },
  { path: 'learning/schools/:schoolId/classes', component: LearningSchoolComponent },
  { path: 'learning/schools/:schoolId/curriculum', component: LearningSchoolComponent },
  { path: 'learning/schools/:schoolId/work', component: LearningSchoolComponent },
  { path: 'admin-screen', component: AdminScreenComponent },
  { path: 'school/admin/learners', component: LearnerManagementComponent },
  { path: 'school/admin/learners/:id', component: LearnerDetailComponent },
  { path: 'school/admin/classes/:id', component: ClassDetailComponent },
  { path: 'school/admin/curriculum', component: CurriculumViewComponent },
  { path: 'school/admin/work', component: WorkManagementComponent },
  { path: 'operator-admin/:operator', component: OperatorAdminComponent },
  { path: 'zimra-guide-2026', component: ZimraComponent },
  { path: 'bus-fares-jhb-bulawayo', component: FaresComponent },
  // ...other routes
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }