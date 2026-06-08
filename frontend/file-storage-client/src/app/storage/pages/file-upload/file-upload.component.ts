import { HttpEvent, HttpEventType } from '@angular/common/http';
import { Component } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ToastService } from '../../../shared/services/toast.service';
import { StoredFile } from '../../models/file.models';
import { FileApiService } from '../../services/file-api.service';

type UploadMode = 'standard' | 'resumable';

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
  failedUploadMode: UploadMode = 'standard';
  isDragging = false;
  isUploading = false;
  uploadStatus = '';
  uploadProgress = 0;

  readonly form = this.formBuilder.nonNullable.group({
    tags: [''],
    uploadMode: ['standard' as UploadMode],
    chunkSizeKb: [512]
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

    if (this.form.controls.uploadMode.value === 'resumable') {
      void this.startResumableUpload(this.selectedFile, this.form.controls.tags.value);
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
    this.form.controls.uploadMode.setValue(this.failedUploadMode);

    if (this.failedUploadMode === 'resumable') {
      void this.startResumableUpload(this.failedFile, this.failedTags);
      return;
    }

    this.startUpload(this.failedFile, this.failedTags);
  }

  clearSelection(): void {
    this.setSelectedFile(null);
    this.uploadProgress = 0;
    this.uploadStatus = '';
  }

  private setSelectedFile(file: File | null): void {
    this.selectedFile = file;
    this.uploadProgress = 0;
    this.uploadStatus = '';
  }

  private startUpload(file: File, tags: string): void {
    this.isUploading = true;
    this.uploadProgress = 0;
    this.uploadStatus = 'Uploading file';
    this.failedFile = null;
    this.failedTags = '';
    this.failedUploadMode = 'standard';

    this.fileApiService.uploadFile(file, tags).subscribe({
      next: (event) => this.handleUploadEvent(event),
      error: () => {
        this.failedFile = file;
        this.failedTags = tags;
        this.failedUploadMode = 'standard';
        this.isUploading = false;
        this.uploadStatus = 'Upload failed';
        this.toastService.error('Upload failed. You can retry the same file.');
      }
    });
  }

  private async startResumableUpload(file: File, tags: string): Promise<void> {
    this.isUploading = true;
    this.uploadProgress = 0;
    this.uploadStatus = 'Creating upload session';
    this.failedFile = null;
    this.failedTags = '';
    this.failedUploadMode = 'resumable';

    try {
      const session = await firstValueFrom(this.fileApiService.createUploadSession({
        originalName: file.name,
        contentType: file.type || 'application/octet-stream',
        totalSizeBytes: file.size,
        tags: this.parseTags(tags)
      }));
      const chunkSize = this.getChunkSizeBytes();
      let uploadedBytes = session.uploadedBytes;

      while (uploadedBytes < file.size) {
        const chunkEnd = Math.min(uploadedBytes + chunkSize, file.size);
        const chunk = file.slice(uploadedBytes, chunkEnd);
        this.uploadStatus = `Uploading chunk ${uploadedBytes} - ${chunkEnd}`;

        const updatedSession = await firstValueFrom(
          this.fileApiService.appendUploadChunk(session.id, chunk, uploadedBytes)
        );

        uploadedBytes = updatedSession.uploadedBytes;
        this.uploadProgress = Math.round((uploadedBytes / file.size) * 100);
      }

      this.uploadStatus = 'Completing upload';
      const storedFile = await firstValueFrom(this.fileApiService.completeUploadSession(session.id));
      this.isUploading = false;
      this.uploadProgress = 100;
      this.uploadStatus = 'Upload complete';
      this.toastService.success(`${storedFile.originalName} was uploaded with resumable upload.`);
      this.router.navigateByUrl('/storage/files');
    } catch {
      this.failedFile = file;
      this.failedTags = tags;
      this.failedUploadMode = 'resumable';
      this.isUploading = false;
      this.uploadStatus = 'Upload failed';
      this.toastService.error('Resumable upload failed. You can retry the same file.');
    }
  }

  private handleUploadEvent(event: HttpEvent<StoredFile>): void {
    if (event.type === HttpEventType.UploadProgress && event.total) {
      this.uploadProgress = Math.round((event.loaded / event.total) * 100);
      return;
    }

    if (event.type === HttpEventType.Response) {
      this.isUploading = false;
      this.uploadProgress = 100;
      this.uploadStatus = 'Upload complete';
      this.toastService.success(`${event.body?.originalName || 'File'} was uploaded successfully.`);
      this.router.navigateByUrl('/storage/files');
    }
  }

  private getChunkSizeBytes(): number {
    const chunkSizeKb = this.form.controls.chunkSizeKb.value;
    const safeChunkSizeKb = Math.max(chunkSizeKb, 64);

    return safeChunkSizeKb * 1024;
  }

  private parseTags(tags: string): string[] {
    if (!tags.trim()) {
      return [];
    }

    return tags
      .split(',')
      .map((tag) => tag.trim())
      .filter(Boolean);
  }
}
