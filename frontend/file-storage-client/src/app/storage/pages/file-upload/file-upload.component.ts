import { HttpEvent, HttpEventType } from '@angular/common/http';
import { Component } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ToastService } from '../../../shared/services/toast.service';
import { StoredFile } from '../../models/file.models';
import { FileApiService } from '../../services/file-api.service';

@Component({
  selector: 'app-file-upload',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './file-upload.component.html',
  styleUrl: './file-upload.component.scss'
})
export class FileUploadComponent {
  selectedFile: File | null = null;
  failedFile: File | null = null;
  failedTags = '';
  isDragging = false;
  isUploading = false;
  uploadProgress = 0;

  readonly form = this.formBuilder.nonNullable.group({
    tags: ['']
  });

  constructor(
    private readonly fileApiService: FileApiService,
    private readonly formBuilder: FormBuilder,
    private readonly router: Router,
    private readonly toastService: ToastService
  ) {}

  onFileInputChanged(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.setSelectedFile(input.files?.item(0) ?? null);
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDragging = true;
  }

  onDragLeave(): void {
    this.isDragging = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragging = false;
    this.setSelectedFile(event.dataTransfer?.files.item(0) ?? null);
  }

  upload(): void {
    if (!this.selectedFile) {
      this.toastService.error('Choose a file before uploading.');
      return;
    }

    this.startUpload(this.selectedFile, this.form.controls.tags.value);
  }

  retryUpload(): void {
    if (!this.failedFile) {
      return;
    }

    this.setSelectedFile(this.failedFile);
    this.form.controls.tags.setValue(this.failedTags);
    this.startUpload(this.failedFile, this.failedTags);
  }

  clearSelection(): void {
    this.setSelectedFile(null);
    this.uploadProgress = 0;
  }

  private setSelectedFile(file: File | null): void {
    this.selectedFile = file;
    this.uploadProgress = 0;
  }

  private startUpload(file: File, tags: string): void {
    this.isUploading = true;
    this.uploadProgress = 0;
    this.failedFile = null;
    this.failedTags = '';

    this.fileApiService.uploadFile(file, tags).subscribe({
      next: (event) => this.handleUploadEvent(event),
      error: () => {
        this.failedFile = file;
        this.failedTags = tags;
        this.isUploading = false;
        this.toastService.error('Upload failed. You can retry the same file.');
      }
    });
  }

  private handleUploadEvent(event: HttpEvent<StoredFile>): void {
    if (event.type === HttpEventType.UploadProgress && event.total) {
      this.uploadProgress = Math.round((event.loaded / event.total) * 100);
      return;
    }

    if (event.type === HttpEventType.Response) {
      this.isUploading = false;
      this.uploadProgress = 100;
      this.toastService.success(`${event.body?.originalName || 'File'} was uploaded successfully.`);
      this.router.navigateByUrl('/storage/files');
    }
  }
}
