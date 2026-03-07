import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-contact',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="min-h-screen flex flex-col items-center justify-center bg-[#0B1020] text-white px-4 relative" style="z-index:1001;">
            <ng-container *ngIf="close">
          <button (click)="close()" aria-label="Close" class="absolute top-4 right-4 bg-[#222] hover:bg-[#444] text-white rounded-full w-10 h-10 flex items-center justify-center text-2xl shadow-lg z-[1100] border-2 border-white">&times;</button>
        </ng-container>  
                <h2 class="text-4xl font-bold mb-6 text-center">Contact Us</h2>
        <p class="text-lg mb-8 max-w-xl text-center mx-auto">We're here to help! Reach out for quotes, support, or general inquiries about our logistics services across Africa.</p>
    <div class="w-full max-w-md bg-[#181F2A] rounded-2xl p-8 flex flex-col gap-4 shadow-lg relative">
        <form class="flex flex-col gap-4">
          <input type="text" placeholder="Your Name" class="px-4 py-3 rounded-lg bg-white text-gray-700 outline-none" required>
          <input type="email" placeholder="Your Email" class="px-4 py-3 rounded-lg bg-white text-gray-700 outline-none" required>
          <textarea placeholder="Your Message" rows="5" class="px-4 py-3 rounded-lg bg-white text-gray-700 outline-none" required></textarea>
          <button type="submit" class="bg-[#22C55E] text-white font-semibold px-8 py-4 rounded-lg text-lg hover:bg-[#16A34A] transition">Send Message</button>
        </form>
        <div class="mt-10 text-gray-300 text-center">
          <p>Email: <a href="mailto:info@pro-klogistics.co.za" class="text-[#22C55E] underline">info@pro-klogistics.co.za</a></p>
          <p>Phone: <a href="tel:+27123456789" class="text-[#22C55E] underline">+27 12 345 6789</a></p>
          <p>Address: 123 Logistics Ave, Johannesburg, South Africa</p>
        </div>
      </div>
    </div>
  `,
  styles: []
})
export class ContactComponent {
  @Input() close?: () => void;
}
