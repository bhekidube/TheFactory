import { ApplicationConfig, provideBrowserGlobalErrorListeners, APP_INITIALIZER } from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { ThemeService } from './theme.service';

export function loadSiteConfig(themeService: ThemeService) {
  return () => themeService.loadConfig();
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    ThemeService,
    {
      provide: APP_INITIALIZER,
      useFactory: loadSiteConfig,
      deps: [ThemeService],
      multi: true
    }
  ]
};
