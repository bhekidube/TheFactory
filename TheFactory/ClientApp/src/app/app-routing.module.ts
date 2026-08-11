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

const routes: Routes = [
  { path: '', component: AuthComponent, pathMatch: 'full' },
  { path: 'auth', component: AuthComponent },
  { path: 'learning', component: LearningComponent },
  { path: 'learning/schools/:schoolId', redirectTo: 'learning/schools/:schoolId/learners', pathMatch: 'full' },
  { path: 'learning/schools/:schoolId/learners', component: LearningSchoolComponent },
  { path: 'learning/schools/:schoolId/reports', component: LearningSchoolComponent },
  { path: 'admin-screen', component: AdminScreenComponent },
  { path: 'school/admin/learners', component: LearnerManagementComponent },
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