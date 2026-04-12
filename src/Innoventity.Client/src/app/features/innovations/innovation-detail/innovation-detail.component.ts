import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { InnovationsService } from '../../../core/services/innovations.service';
import { InnovationDetail } from '../../../core/models/innovation.model';

@Component({
  selector: 'app-innovation-detail',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatProgressSpinnerModule,
    MatChipsModule,
    MatButtonModule,
    MatIconModule
  ],
  templateUrl: './innovation-detail.component.html',
  styleUrl: './innovation-detail.component.scss'
})
export class InnovationDetailComponent implements OnInit {
  innovation = signal<InnovationDetail | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private innovationsService: InnovationsService
  ) {}

  async ngOnInit(): Promise<void> {
    const id = this.route.snapshot.paramMap.get('id');

    if (!id) {
      this.error.set('Invalid innovation ID');
      this.loading.set(false);
      return;
    }

    const result = await this.innovationsService.getInnovationById(id);

    if (result.success && result.data) {
      this.innovation.set(result.data);
    } else {
      this.error.set(result.error || 'Failed to load innovation');
    }

    this.loading.set(false);
  }

  goBack(): void {
    this.router.navigate(['/']);
  }
}
