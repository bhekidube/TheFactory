import { Injectable } from '@angular/core';
import { SiteConfig } from './site-config';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  config!: SiteConfig;

  loadConfig(): Promise<void> {
    const hostname = window.location.hostname;
    const configUrl = `/assets/config.${hostname}.json`;
    return fetch(configUrl)
      .then(res => res.json())
      .then((data: SiteConfig) => {
        this.config = data;
        document.documentElement.style.setProperty('--primary-color', data.primaryColor);
        document.title = data.brandName;
      });
  }
}
