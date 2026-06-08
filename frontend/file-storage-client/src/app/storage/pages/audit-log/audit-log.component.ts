import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ToastService } from '../../../shared/services/toast.service';
import { AuditAction, AuditLog } from '../../models/audit-log.models';
import { AuditLogApiService } from '../../services/audit-log-api.service';

@Component({
  selector: 'app-audit-log',
  standalone: true,
  imports: [DatePipe, DecimalPipe, ReactiveFormsModule, RouterLink],
  templateUrl: './audit-log.component.html',
  styleUrl: './audit-log.component.scss'
})
export class AuditLogComponent implements OnInit {
  auditLogs: AuditLog[] = [];
  isLoading = false;
  totalCount = 0;
  pageNumber = 1;

  readonly actions: AuditAction[] = ['Upload', 'Download', 'Preview', 'SoftDelete', 'HardDelete'];

  readonly filtersForm = this.formBuilder.nonNullable.group({
    fileId: [''],
    action: [''],
    userId: [''],
    from: [''],
    to: [''],
    pageSize: [10]
  });

  constructor(
    private readonly auditLogApiService: AuditLogApiService,
    private readonly formBuilder: FormBuilder,
    private readonly toastService: ToastService
  ) {}

  get totalPages(): number {
    return Math.max(Math.ceil(this.totalCount / this.filtersForm.controls.pageSize.value), 1);
  }

  ngOnInit(): void {
    this.loadAuditLogs();
  }

  loadAuditLogs(): void {
    this.isLoading = true;

    this.auditLogApiService.searchAuditLogs(this.createFilters()).subscribe({
      next: (response) => {
        this.auditLogs = response.items;
        this.totalCount = response.totalCount;
        this.pageNumber = response.pageNumber;
        this.isLoading = false;
      },
      error: () => {
        this.toastService.error('Could not load audit logs. Admin role is required.');
        this.isLoading = false;
      }
    });
  }

  applyFilters(): void {
    this.pageNumber = 1;
    this.loadAuditLogs();
  }

  clearFilters(): void {
    this.filtersForm.reset({
      fileId: '',
      action: '',
      userId: '',
      from: '',
      to: '',
      pageSize: 10
    });
    this.pageNumber = 1;
    this.loadAuditLogs();
  }

  goToPreviousPage(): void {
    if (this.pageNumber <= 1) {
      return;
    }

    this.pageNumber--;
    this.loadAuditLogs();
  }

  goToNextPage(): void {
    if (this.pageNumber >= this.totalPages) {
      return;
    }

    this.pageNumber++;
    this.loadAuditLogs();
  }

  private createFilters() {
    const filters = this.filtersForm.getRawValue();

    return {
      pageNumber: this.pageNumber,
      pageSize: filters.pageSize,
      fileId: filters.fileId,
      action: filters.action,
      userId: filters.userId,
      fromUtc: this.toStartOfDayUtc(filters.from),
      toUtc: this.toEndOfDayUtc(filters.to)
    };
  }

  private toStartOfDayUtc(value: string): string | undefined {
    return value ? `${value}T00:00:00.000Z` : undefined;
  }

  private toEndOfDayUtc(value: string): string | undefined {
    return value ? `${value}T23:59:59.999Z` : undefined;
  }
}
