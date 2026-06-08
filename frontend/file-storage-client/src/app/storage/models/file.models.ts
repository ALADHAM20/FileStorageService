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

export interface FileSearchFilters {
  pageNumber: number;
  pageSize: number;
  name?: string;
  tag?: string;
  contentType?: string;
  createdFromUtc?: string;
  createdToUtc?: string;
}

export interface ApiResponse {
  statusCode: number;
  message: string;
}
