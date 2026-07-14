import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthComponent } from './auth/auth.component'; 
import { AdminScreenComponent } from './admin-screen/admin-screen.component';
import { OperatorAdminComponent } from './operator-admin/operator-admin.component';
import { ZimraComponent } from './zimra/zimra.component';
import { FaresComponent } from './fares/fares.component';

const routes: Routes = [
  { path: '', component: AuthComponent, pathMatch: 'full' },
  { path: 'auth', component: AuthComponent },
  { path: 'admin-screen', component: AdminScreenComponent },
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