export type AuditAction = 'Upload' | 'Download' | 'Preview' | 'SoftDelete' | 'HardDelete';

export interface AuditLog {
  id: string;
  fileId: string | null;
  action: AuditAction;
  userId: string;
  occurredAtUtc: string;
  ipAddress: string;
  correlationId: string;
  details: string;
}

export interface AuditLogSearchFilters {
  pageNumber: number;
  pageSize: number;
  fileId?: string;
  action?: string;
  userId?: string;
  fromUtc?: string;
  toUtc?: string;
}
