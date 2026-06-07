import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FileApiService, StoredFile } from '../../core/services/file-api.service';

@Component({
  selector: 'app-files',
  standalone: true,
  imports: [DatePipe, DecimalPipe],
  templateUrl: './files.component.html',
  styleUrl: './files.component.scss'
})
export class FilesComponent implements OnInit {
  files: StoredFile[] = [];
  isLoading = false;
  errorMessage = '';

  constructor(private readonly fileApiService: FileApiService) {}

  ngOnInit(): void {
    this.loadFiles();
  }

  loadFiles(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.fileApiService.searchFiles().subscribe({
      next: (response) => {
        this.files = response.items;
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'Could not load files. Make sure you are logged in and the API is running.';
        this.isLoading = false;
      }
    });
  }
}
