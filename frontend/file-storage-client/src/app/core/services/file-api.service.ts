import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

export interface StoredFile {
  id: string;
  originalName: string;
  storedKey: string;
  sizeBytes: number;
  contentType: string;
  sha256Checksum: string;
  tags: string[];
  createdAtUtc: string;
  deletedAtUtc: string | null;
  version: number | null;
  createdByUserId: string;
}

export interface PagedResponse<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
}

@Injectable({
  providedIn: 'root'
})
export class FileApiService {
  constructor(
    private readonly http: HttpClient,
    private readonly authService: AuthService
  ) {}

  searchFiles(): Observable<PagedResponse<StoredFile>> {
    return this.http.get<PagedResponse<StoredFile>>(
      `${environment.apiBaseUrl}/api/files`,
      { headers: this.createAuthHeaders() }
    );
  }

  private createAuthHeaders(): HttpHeaders {
    const token = this.authService.token;

    return token
      ? new HttpHeaders({ Authorization: `Bearer ${token}` })
      : new HttpHeaders();
  }
}
