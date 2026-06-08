import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResponse } from '../models/file.models';
import { AuditLog, AuditLogSearchFilters } from '../models/audit-log.models';

@Injectable({
  providedIn: 'root'
})
export class AuditLogApiService {
  private readonly auditLogsUrl = `${environment.apiBaseUrl}/api/audit-logs`;

  constructor(private readonly http: HttpClient) {}

  searchAuditLogs(filters: AuditLogSearchFilters): Observable<PagedResponse<AuditLog>> {
    return this.http.get<PagedResponse<AuditLog>>(this.auditLogsUrl, {
      params: this.createSearchParams(filters)
    });
  }

  private createSearchParams(filters: AuditLogSearchFilters): HttpParams {
    let params = new HttpParams()
      .set('pageNumber', String(filters.pageNumber))
      .set('pageSize', String(filters.pageSize));

    if (filters.fileId?.trim()) {
      params = params.set('fileId', filters.fileId.trim());
    }

    if (filters.action?.trim()) {
      params = params.set('action', filters.action.trim());
    }

    if (filters.userId?.trim()) {
      params = params.set('userId', filters.userId.trim());
    }

    if (filters.fromUtc) {
      params = params.set('fromUtc', filters.fromUtc);
    }

    if (filters.toUtc) {
      params = params.set('toUtc', filters.toUtc);
    }

    return params;
  }
}
