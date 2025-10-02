import { Component, Input } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-operator-admin',
  templateUrl: './operator-admin.component.html',
  styleUrls: ['./operator-admin.component.css']
})
export class OperatorAdminComponent {
  @Input() operatorName: string = '';
  @Input() summary: any;

  constructor(private route: ActivatedRoute) {}

  ngOnInit(): void {
    this.operatorName = this.route.snapshot.paramMap.get('operator') || '';
  }
  
  userRole: string = localStorage.getItem('userRole') || 'Public';
  isLoggedIn: boolean = !!localStorage.getItem('userName');
}
