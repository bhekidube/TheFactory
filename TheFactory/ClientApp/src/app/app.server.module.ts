import { NgModule } from '@angular/core';
import { ServerModule } from '@angular/platform-server';
// Removed ModuleMapLoaderModule for latest Angular SSR
import { AppComponent } from './app.component';
import { AppModule } from './app.module';

@NgModule({
    imports: [AppModule, ServerModule],
    bootstrap: [AppComponent]
})
export class AppServerModule { }

