import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthComponent } from './auth/auth.component'; 
import { AdminScreenComponent } from './admin-screen/admin-screen.component';
import { OperatorAdminComponent } from './operator-admin/operator-admin.component';

const routes: Routes = [
  { path: '', component: AuthComponent, pathMatch: 'full' },
  { path: 'auth', component: AuthComponent },
  { path: 'admin-screen', component: AdminScreenComponent },
  { path: 'operator-admin/:operator', component: OperatorAdminComponent }
  // ...other routes
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }