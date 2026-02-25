import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ThemeService } from './theme.service';

@Component({
  selector: 'app-banner',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div *ngIf="themeService.config.features['showBanner']" style="min-height: 100vh; display: flex; flex-direction: column; justify-content: center; align-items: center;">
      <h1 style="text-align: center;">{{ themeService.config.brandName }}</h1>
      <img [src]="themeService.config.logoUrl" alt="Logo" style="max-width:40vw; height:auto; display:block; margin:auto;">
      <footer style="position:fixed; left:0; bottom:0; width:100vw; background:rgba(255,255,255,0.95); text-align:center; color:#888; font-size:1rem; padding:0.75rem 0; z-index:100; box-shadow:0 -1px 4px rgba(0,0,0,0.04);">
        Powered by <a href=\"https://www.hambaonline.com\" target=\"_blank\" rel=\"noopener\" style=\"color:inherit; text-decoration:underline;\">www.hambaonline.com</a>
      </footer>
    </div>
  `
})
export class BannerComponent {
  constructor(public themeService: ThemeService) {}
}
