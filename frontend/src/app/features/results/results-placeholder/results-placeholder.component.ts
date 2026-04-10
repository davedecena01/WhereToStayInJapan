import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-results-placeholder',
  imports: [RouterLink],
  template: `
    <div class="placeholder">
      <h1>Recommendations</h1>
      <p>The recommendation engine is coming in Phase 2.</p>
      <a routerLink="/review">← Back to review</a>
    </div>
  `,
  styles: [`
    .placeholder {
      max-width: 600px;
      margin: 4rem auto;
      text-align: center;
      color: var(--color-navy);

      p { color: #666; margin: 1rem 0; }
      a { color: var(--color-sakura); text-decoration: underline; }
    }
  `]
})
export class ResultsPlaceholderComponent {}
