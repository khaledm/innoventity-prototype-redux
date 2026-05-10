import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { InnovationsService } from '../../../core/services/innovations.service';
import { InnovationListItem } from '../../../core/models/innovation.model';

@Component({
  selector: 'app-innovations-list',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatChipsModule,
    MatProgressSpinnerModule,
    MatIconModule
  ],
  templateUrl: './innovations-list.component.html',
  styleUrl: './innovations-list.component.scss'
})
export class InnovationsListComponent implements OnInit {
  innovations = signal<InnovationListItem[]>([]);
  totalCount = signal(0);
  loading = signal(true);
  error = signal<string | null>(null);

  constructor(
    private innovationsService: InnovationsService,
    private router: Router
  ) {}

  async ngOnInit(): Promise<void> {
    const result = await this.innovationsService.listInnovations();

    if (result.success && result.data) {
      this.innovations.set(result.data.items);
      this.totalCount.set(result.data.totalCount);
    } else {
      this.error.set(result.error ?? 'Failed to load innovations.');
    }

    this.loading.set(false);
  }

  viewDetail(innovationId: string): void {
    this.router.navigate(['/innovations', innovationId]);
  }
}
