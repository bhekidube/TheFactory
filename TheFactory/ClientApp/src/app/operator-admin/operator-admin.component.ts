import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-operator-admin',
  templateUrl: './operator-admin.component.html',
  styleUrls: ['./operator-admin.component.css']
})
export class OperatorAdminComponent {
  @Input() operatorName: string = '';
  @Input() summary: any;

  userRole: string = localStorage.getItem('userRole') || 'Public';
  isLoggedIn: boolean = !!localStorage.getItem('userName');
}
