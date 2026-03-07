import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-contact',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="min-h-screen flex flex-col items-center justify-center bg-[#0B1020] text-white px-2 sm:px-4 relative" style="z-index:1001;">
      <div class="w-full max-w-md bg-[#181F2A] rounded-2xl p-4 sm:p-8 flex flex-col gap-4 shadow-lg relative">
        <ng-container *ngIf="close">
          <button (click)="close()" aria-label="Close" class="absolute top-2 sm:top-4 right-2 sm:right-4 bg-[#222] hover:bg-[#444] text-white rounded-full w-8 sm:w-10 h-8 sm:h-10 flex items-center justify-center text-xl sm:text-2xl shadow-lg z-[1100] border-2 border-white">&times;</button>
        </ng-container>
        <h2 class="text-2xl sm:text-4xl font-bold mb-4 sm:mb-6 text-center">Contact Us</h2>
        <p class="text-base sm:text-lg mb-6 sm:mb-8 max-w-xl text-center mx-auto">We're here to help! Reach out for quotes, support, or general inquiries about our logistics services across Africa.</p>
        <form class="flex flex-col gap-3 sm:gap-4">
          <input type="text" placeholder="Your Name" class="px-3 sm:px-4 py-2 sm:py-3 rounded-lg bg-white text-gray-700 outline-none text-base sm:text-lg" required>
          <input type="email" placeholder="Your Email" class="px-3 sm:px-4 py-2 sm:py-3 rounded-lg bg-white text-gray-700 outline-none text-base sm:text-lg" required>
          <textarea placeholder="Your Message" rows="4" class="px-3 sm:px-4 py-2 sm:py-3 rounded-lg bg-white text-gray-700 outline-none text-base sm:text-lg" required></textarea>
          <button type="submit" class="bg-[#22C55E] text-white font-semibold px-6 sm:px-8 py-3 sm:py-4 rounded-lg text-base sm:text-lg hover:bg-[#16A34A] transition">Send Message</button>
        </form>
        <div class="mt-6 sm:mt-10 text-gray-300 text-center text-sm sm:text-base">
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
