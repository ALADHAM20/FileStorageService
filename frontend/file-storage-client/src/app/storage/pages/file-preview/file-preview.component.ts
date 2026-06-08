import { Component, OnDestroy, OnInit } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ToastService } from '../../../shared/services/toast.service';
import { FileApiService } from '../../services/file-api.service';

@Component({
  selector: 'app-file-preview',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './file-preview.component.html',
  styleUrl: './file-preview.component.scss'
})
export class FilePreviewComponent implements OnInit, OnDestroy {
  fileName = 'File preview';
  contentType = '';
  previewUrl = '';
  previewSafeUrl: SafeResourceUrl | null = null;
  isLoading = false;

  constructor(
    private readonly fileApiService: FileApiService,
    private readonly route: ActivatedRoute,
    private readonly sanitizer: DomSanitizer,
    private readonly toastService: ToastService
  ) {}

  get isImage(): boolean {
    return this.contentType.toLowerCase().startsWith('image/');
  }

  ngOnInit(): void {
    this.fileName = this.route.snapshot.queryParamMap.get('name') || this.fileName;
    const id = this.route.snapshot.paramMap.get('id');

    if (!id) {
      this.toastService.error('File id is missing.');
      return;
    }

    this.loadPreview(id);
  }

  ngOnDestroy(): void {
    this.revokePreviewUrl();
  }

  private loadPreview(id: string): void {
    this.isLoading = true;

    this.fileApiService.previewFile(id).subscribe({
      next: (response) => {
        if (!response.body) {
          this.toastService.error('Preview content was empty.');
          this.isLoading = false;
          return;
        }

        this.contentType = response.body.type || response.headers.get('content-type') || '';
        this.previewUrl = URL.createObjectURL(response.body);
        this.previewSafeUrl = this.sanitizer.bypassSecurityTrustResourceUrl(this.previewUrl);
        this.isLoading = false;
      },
      error: () => {
        this.toastService.error('Preview is not available for this file.');
        this.isLoading = false;
      }
    });
  }

  private revokePreviewUrl(): void {
    if (this.previewUrl) {
      URL.revokeObjectURL(this.previewUrl);
    }
  }
}
