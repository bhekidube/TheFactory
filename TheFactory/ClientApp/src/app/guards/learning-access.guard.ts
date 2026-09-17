import { Injectable } from '@angular/core';
import { CanActivate, CanActivateChild, Router, UrlTree } from '@angular/router';

@Injectable({
  providedIn: 'root'
})
export class LearningAccessGuard implements CanActivate, CanActivateChild {
  private static readonly allowedRoles = new Set<string>([
    'SystemAdmin',
    'Admin',
    'OperatorAdmin',
    'Operator'
  ]);

  constructor(private readonly router: Router) {}

  canActivate(): boolean | UrlTree {
    return this.checkAccess();
  }

  canActivateChild(): boolean | UrlTree {
    return this.checkAccess();
  }

  private checkAccess(): boolean | UrlTree {
    const role = (localStorage.getItem('userRole') || '').trim();
    const userId = (localStorage.getItem('userId') || '').trim();

    if (!userId || !LearningAccessGuard.allowedRoles.has(role)) {
      return this.router.parseUrl('/');
    }

    return true;
  }
}
