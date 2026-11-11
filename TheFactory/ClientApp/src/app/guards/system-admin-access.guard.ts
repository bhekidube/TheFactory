import { Injectable } from '@angular/core';
import { CanActivate, Router, UrlTree } from '@angular/router';

@Injectable({
  providedIn: 'root'
})
export class SystemAdminAccessGuard implements CanActivate {
  constructor(private readonly router: Router) {}

  canActivate(): boolean | UrlTree {
    if (typeof localStorage === 'undefined') {
      return this.router.parseUrl('/');
    }

    const userId = (localStorage.getItem('userId') || '').trim();
    const role = (localStorage.getItem('userRole') || '').trim();

    return userId && role === 'SystemAdmin'
      ? true
      : this.router.parseUrl('/');
  }
}
