import { Component } from '@angular/core';
import { AnimationOptions } from 'ngx-lottie';

@Component({
  selector: 'app-mascot-avatar',
  templateUrl: './mascot-avatar.component.html',
  styleUrls: ['./mascot-avatar.component.css']
})
export class MascotAvatarComponent {
  lottieOptions: AnimationOptions = {
    path: 'assets/RunningBall.lottie.json',
    loop: true,
    autoplay: true,
  };
}
