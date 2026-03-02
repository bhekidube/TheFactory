import { ApplicationConfig, provideBrowserGlobalErrorListeners, APP_INITIALIZER, importProvidersFrom } from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { ThemeService } from './theme.service';
import { LucideAngularModule, MapPin, Truck, Plane, Ship } from 'lucide-angular';

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
    },
    importProvidersFrom(LucideAngularModule.pick({ MapPin, Truck, Plane, Ship }))
  ]
};
