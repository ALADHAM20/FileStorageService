import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../shared/services/toast.service';
import { StoredFile } from '../../models/file.models';
import { FileApiService } from '../../services/file-api.service';

@Component({
  selector: 'app-file-list',
  standalone: true,
  imports: [DatePipe, DecimalPipe, ReactiveFormsModule, RouterLink],
  templateUrl: './file-list.component.html',
  styleUrl: './file-list.component.scss'
})
export class FileListComponent implements OnInit {
  files: StoredFile[] = [];
  isLoading = false;
  totalCount = 0;
  pageNumber = 1;

  readonly filtersForm = this.formBuilder.nonNullable.group({
    name: [''],
    tag: [''],
    contentType: [''],
    createdFrom: [''],
    createdTo: [''],
    pageSize: [10]
  });

  constructor(
    public readonly authService: AuthService,
    private readonly fileApiService: FileApiService,
    private readonly formBuilder: FormBuilder,
    private readonly toastService: ToastService
  ) {}

  get isAdmin(): boolean {
    return this.authService.role === 'admin';
  }

  get totalPages(): number {
    return Math.max(Math.ceil(this.totalCount / this.filtersForm.controls.pageSize.value), 1);
  }

  ngOnInit(): void {
    this.loadFiles();
  }

  loadFiles(): void {
    this.isLoading = true;

    this.fileApiService.searchFiles(this.createFilters()).subscribe({
      next: (response) => {
        this.files = response.items;
        this.totalCount = response.totalCount;
        this.pageNumber = response.pageNumber;
        this.isLoading = false;
      },
      error: () => {
        this.toastService.error('Could not load files. Make sure the API is running.');
        this.isLoading = false;
      }
    });
  }

  applyFilters(): void {
    this.pageNumber = 1;
    this.loadFiles();
  }

  clearFilters(): void {
    this.filtersForm.reset({
      name: '',
      tag: '',
      contentType: '',
      createdFrom: '',
      createdTo: '',
      pageSize: 10
    });
    this.pageNumber = 1;
    this.loadFiles();
  }

  goToPreviousPage(): void {
    if (this.pageNumber <= 1) {
      return;
    }

    this.pageNumber--;
    this.loadFiles();
  }

  goToNextPage(): void {
    if (this.pageNumber >= this.totalPages) {
      return;
    }

    this.pageNumber++;
    this.loadFiles();
  }

  downloadFile(file: StoredFile): void {
    this.fileApiService.downloadFile(file.id).subscribe({
      next: (response) => {
        this.saveBlob(response.body, file.originalName);
        this.toastService.success(`${file.originalName} download started.`);
      },
      error: () => {
        this.toastService.error(`Could not download ${file.originalName}.`);
      }
    });
  }

  softDeleteFile(file: StoredFile): void {
    this.fileApiService.softDeleteFile(file.id).subscribe({
      next: (response) => {
        this.toastService.success(response.message);
        this.loadFiles();
      },
      error: () => {
        this.toastService.error(`Could not delete ${file.originalName}.`);
      }
    });
  }

  hardDeleteFile(file: StoredFile): void {
    this.fileApiService.hardDeleteFile(file.id).subscribe({
      next: (response) => {
        this.toastService.success(response.message);
        this.loadFiles();
      },
      error: () => {
        this.toastService.error('Could not permanently delete the file. Admin role is required.');
      }
    });
  }

  canPreview(file: StoredFile): boolean {
    return file.contentType.toLowerCase().startsWith('image/')
      || file.contentType.toLowerCase() === 'application/pdf';
  }

  private createFilters() {
    const filters = this.filtersForm.getRawValue();

    return {
      pageNumber: this.pageNumber,
      pageSize: filters.pageSize,
      name: filters.name,
      tag: filters.tag,
      contentType: filters.contentType,
      createdFromUtc: this.toStartOfDayUtc(filters.createdFrom),
      createdToUtc: this.toEndOfDayUtc(filters.createdTo)
    };
  }

  private toStartOfDayUtc(value: string): string | undefined {
    return value ? `${value}T00:00:00.000Z` : undefined;
  }

  private toEndOfDayUtc(value: string): string | undefined {
    return value ? `${value}T23:59:59.999Z` : undefined;
  }

  private saveBlob(blob: Blob | null, fileName: string): void {
    if (!blob) {
      return;
    }

    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.click();
    URL.revokeObjectURL(url);
  }
}
