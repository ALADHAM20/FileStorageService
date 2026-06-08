import { HttpClient, HttpEvent, HttpParams, HttpResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, FileSearchFilters, PagedResponse, StoredFile } from '../models/file.models';

@Injectable({
  providedIn: 'root'
})
export class FileApiService {
  private readonly filesUrl = `${environment.apiBaseUrl}/api/files`;

  constructor(private readonly http: HttpClient) {}

  searchFiles(filters: FileSearchFilters): Observable<PagedResponse<StoredFile>> {
    return this.http.get<PagedResponse<StoredFile>>(this.filesUrl, {
      params: this.createSearchParams(filters)
    });
  }

  uploadFile(file: File, tags: string): Observable<HttpEvent<StoredFile>> {
    const formData = new FormData();

    if (tags.trim()) {
      formData.append('tags', tags.trim());
    }

    formData.append('file', file);

    return this.http.post<StoredFile>(this.filesUrl, formData, {
      observe: 'events',
      reportProgress: true
    });
  }

  downloadFile(id: string): Observable<HttpResponse<Blob>> {
    return this.http.get(`${this.filesUrl}/${id}/download`, {
      observe: 'response',
      responseType: 'blob'
    });
  }

  previewFile(id: string): Observable<HttpResponse<Blob>> {
    return this.http.get(`${this.filesUrl}/${id}/preview`, {
      observe: 'response',
      responseType: 'blob'
    });
  }

  softDeleteFile(id: string): Observable<ApiResponse> {
    return this.http.delete<ApiResponse>(`${this.filesUrl}/${id}`);
  }

  hardDeleteFile(id: string): Observable<ApiResponse> {
    return this.http.delete<ApiResponse>(`${this.filesUrl}/${id}/hard`);
  }

  private createSearchParams(filters: FileSearchFilters): HttpParams {
    let params = new HttpParams()
      .set('pageNumber', String(filters.pageNumber))
      .set('pageSize', String(filters.pageSize));

    if (filters.name?.trim()) {
      params = params.set('name', filters.name.trim());
    }

    if (filters.tag?.trim()) {
      params = params.set('tag', filters.tag.trim());
    }

    if (filters.contentType?.trim()) {
      params = params.set('contentType', filters.contentType.trim());
    }

    if (filters.createdFromUtc) {
      params = params.set('createdFromUtc', filters.createdFromUtc);
    }

    if (filters.createdToUtc) {
      params = params.set('createdToUtc', filters.createdToUtc);
    }

    return params;
  }
}
