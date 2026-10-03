import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { AppInitializer } from '@core/app/app-initializer';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, NzAlertModule],
  templateUrl: './app.html',
  styleUrl: './app.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  readonly initializer = inject(AppInitializer);
}
