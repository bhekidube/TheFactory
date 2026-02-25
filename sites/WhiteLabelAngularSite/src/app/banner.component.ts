import { Component } from '@angular/core';
import { ThemeService } from './theme.service';

@Component({
  selector: 'app-banner',
  template: `
    <div *ngIf="themeService.config.features.showBanner">
      <h1>{{ themeService.config.brandName }}</h1>
      <img [src]="themeService.config.logoUrl" alt="Logo">
    </div>
  `
})
export class BannerComponent {
  constructor(public themeService: ThemeService) {}
}
