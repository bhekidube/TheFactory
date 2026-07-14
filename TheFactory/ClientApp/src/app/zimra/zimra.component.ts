
import { Component } from '@angular/core';
import { Title, Meta } from '@angular/platform-browser';



@Component({
  selector: 'app-zimra',
  templateUrl: './zimra.component.html',
  styleUrls: ['./zimra.component.css']
})
export class ZimraComponent {
  constructor(private titleService: Title, private metaService: Meta) {}

  ngOnInit() {
  this.titleService.setTitle('ZIMRA Online Form 47 Guide (2026) - HambaOnline');
  this.metaService.updateTag({ name: 'description', content: 'Step-by-step guide for JHB to BYO travelers on filling out ZIMRA customs forms online.' });
  
  // WhatsApp/Facebook Preview Tags
  this.metaService.updateTag({ property: 'og:title', content: 'Crossing Beitbridge? Read the 2026 ZIMRA Guide' });
  this.metaService.updateTag({ property: 'og:image', content: 'https://hambaonline.com/assets/checklist-thumb.jpg' });
}
}
