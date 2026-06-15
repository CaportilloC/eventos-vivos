import { Component, input } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-operational-guide',
  standalone: true,
  imports: [MatCardModule, MatIconModule],
  template: `
    <mat-card class="operational-guide" appearance="outlined">
      <mat-card-content>
        <div class="guide-header">
          <div class="guide-title-block">
            <mat-icon>{{ icon() }}</mat-icon>
            <div>
              <h2>{{ title() }}</h2>
              @if (description()) {
                <p>{{ description() }}</p>
              }
            </div>
          </div>

          @if (badges().length > 0) {
            <div class="guide-badges" aria-label="Etiquetas de la guía operativa">
              @for (badge of badges(); track badge) {
                <span>{{ badge }}</span>
              }
            </div>
          }
        </div>

        <ul class="guide-list">
          @for (item of items(); track item) {
            <li>
              <mat-icon>check_circle</mat-icon>
              <span>{{ item }}</span>
            </li>
          }
        </ul>
      </mat-card-content>
    </mat-card>
  `,
  styles: `
    .operational-guide {
      max-width: 980px;
      margin: 0 auto 20px;
      border-color: rgba(46, 55, 164, 0.12) !important;
      background: linear-gradient(135deg, #ffffff 0%, #f8f9ff 100%) !important;
      border-radius: var(--ev-radius-md) !important;
    }

    .operational-guide mat-card-content {
      padding: 18px 20px;
    }

    .guide-header {
      display: flex;
      justify-content: space-between;
      gap: 16px;
      flex-wrap: wrap;
      margin-bottom: 14px;
    }

    .guide-title-block {
      display: flex;
      align-items: flex-start;
      gap: 12px;
      min-width: 260px;
      flex: 1;
    }

    .guide-title-block > mat-icon {
      color: var(--ev-primary);
      width: 28px;
      height: 28px;
      font-size: 28px;
      flex-shrink: 0;
      margin-top: 2px;
    }

    h2 {
      margin: 0 0 4px;
      color: var(--ev-text-primary);
      font-size: 16px;
      font-weight: 700;
    }

    p {
      margin: 0;
      color: var(--ev-text-secondary);
      font-size: 13px;
      line-height: 1.5;
    }

    .guide-badges {
      display: flex;
      align-items: flex-start;
      flex-wrap: wrap;
      gap: 8px;
    }

    .guide-badges span {
      display: inline-flex;
      align-items: center;
      min-height: 26px;
      padding: 4px 10px;
      border: 1px solid rgba(46, 55, 164, 0.14);
      border-radius: 999px;
      background: #fff;
      color: var(--ev-primary);
      font-size: 12px;
      font-weight: 700;
      white-space: nowrap;
    }

    .guide-list {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      gap: 10px 16px;
      margin: 0;
      padding: 0;
      list-style: none;
    }

    .guide-list li {
      display: flex;
      align-items: flex-start;
      gap: 8px;
      color: var(--ev-text-secondary);
      font-size: 13px;
      line-height: 1.45;
    }

    .guide-list mat-icon {
      width: 17px;
      height: 17px;
      color: #16a34a;
      font-size: 17px;
      flex-shrink: 0;
      margin-top: 1px;
    }

    @media (max-width: 700px) {
      .operational-guide mat-card-content {
        padding: 16px;
      }

      .guide-list {
        grid-template-columns: 1fr;
      }
    }
  `,
})
export class OperationalGuideComponent {
  readonly title = input('Guía operativa');
  readonly description = input('');
  readonly icon = input('tips_and_updates');
  readonly items = input<string[]>([]);
  readonly badges = input<string[]>([]);
}
